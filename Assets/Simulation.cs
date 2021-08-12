using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Simulation : MonoBehaviour{
    public GameObject canvas;
    public RawImage image;
    public ComputeShader shader;
    private int width;
    private int height;

    private RenderTexture[] maps;
    private uint numTex = 2;
    private uint curTex = 0;
    private uint prevTex = 1;

    private int numAgents = 10000000;

    struct Agent{
        public Vector2 position;
        public float angle;
        public int species;
    };
    private ComputeBuffer computeBuffer;
    private Agent[] agents;

    // Start is called before the first frame update
    void Start(){
        width = 1920;//640, 128
        height = 1080;//360, 72

        shader.SetInt("numAgents", numAgents);
        shader.SetInt("width", width);
        shader.SetInt("height", height);

        RenderTexture map = new RenderTexture(width,height,24);
        map.enableRandomWrite = true;
        map.Create();
        map.filterMode = FilterMode.Point;

        RenderTexture map2 = new RenderTexture(width,height,24);
        map2.enableRandomWrite = true;
        map2.Create();
        map2.filterMode = FilterMode.Point;

        maps = new RenderTexture[2];
        maps[0] = map;
        maps[1] = map2;

        int kernelHandle = shader.FindKernel("DrawBlack");
        shader.SetTexture(kernelHandle, "map", maps[curTex]);
        shader.Dispatch(kernelHandle, width/8, height/8, 1);
        image.texture = maps[curTex];

        //create agents
        agents = new Agent[numAgents];

        Vector2 centre = new Vector2(width/2, height/2);

        for (int i = 0; i < numAgents; i++){
            Agent agent = new Agent();

            agent.position = centre + Random.insideUnitCircle * height/10;
            Vector2 diference = agent.position - centre;
            float sign = (centre.y < agent.position.y)? -1.0f : 1.0f;
            agent.angle = Vector2.Angle(Vector2.right, diference) * sign;
            agent.species = Random.Range(0, 2);
            agents[i] = agent;
        }
    }


    // Update is called once per frame
    void Update(){
        prevTex = curTex;
        curTex = (curTex + 1) % numTex;

        simulateAgents();
        simulatePheromones();

        image.texture = maps[curTex];
    }

    void simulateAgents(){
        int kernelHandle = shader.FindKernel("Update");
        shader.SetFloat("deltaTime", Time.deltaTime);

        //set agent list
        computeBuffer = new ComputeBuffer(numAgents, sizeof(float) * 3 + sizeof(int));
        computeBuffer.SetData(agents);
        shader.SetBuffer(kernelHandle, "agents", computeBuffer);

        shader.SetTexture(kernelHandle, "map", maps[prevTex]);
        shader.Dispatch(kernelHandle, numAgents/256, 1, 1);
        computeBuffer.GetData(agents);
        computeBuffer.Release();

    }

    void simulatePheromones(){
        int kernelHandle = shader.FindKernel("ProcessTrailMap");
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.SetTexture(kernelHandle, "map", maps[prevTex]);
        shader.SetTexture(kernelHandle, "nextMap", maps[curTex]);

        shader.Dispatch(kernelHandle, width/16, height/16, 1);
    }
}
