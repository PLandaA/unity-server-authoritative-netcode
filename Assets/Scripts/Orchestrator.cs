using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Orchestrator : MonoBehaviour{ 
    
    [SerializeField] private Slider slider;
    [SerializeField] private Transform cubeA;
    [SerializeField] private Transform cubeB;
    [SerializeField] float speed;
    [SerializeField] private int tickMs = 50;


    [SerializeField] private TMP_Text currentLatency; 
    [SerializeField] private TMP_Text rttText; 
    [SerializeField] private TMP_Text clientAPositionText; 
    [SerializeField] private TMP_Text clientBPositionText; 
    [SerializeField] private TMP_Text serverPositionText; 


    LagQueue<InputMessage>[] inputQueues;
    LagQueue<StateMessage>[] outputQueues;

    private const int Players = 2;
    int latencyMs = 100;
    private int RTT;
    float clocksMs;
    private int nextTickMs;

    private Server server;
    private Client clientA;
    private Client clientB;


    private bool measuring;
    private int pressFrame;
    private float pressTime;
    private float lastX; 

    private void Awake(){
        if (tickMs <= 0){
            Debug.LogError("Tick ms must be greater than zero");
            tickMs = 50;
        }

        inputQueues = new LagQueue<InputMessage>[Players];
        for (int i = 0; i < Players; i++){
            inputQueues[i] = new LagQueue<InputMessage>(latencyMs);
        }

        outputQueues = new LagQueue<StateMessage>[Players];
        for (int i = 0; i < Players; i++){
            outputQueues[i] = new LagQueue<StateMessage>(latencyMs);
        }

        server = new Server(inputQueues, outputQueues, speed);


        clientA = new Client(0, inputQueues[0], outputQueues[0]);
        clientB = new Client(1, inputQueues[1], outputQueues[1]);
    }

    private void Start(){
        RTT = latencyMs * 2; 
        currentLatency.text = $"Latency: {latencyMs}"; 
        rttText.text = $"RTT: {RTT}"; 
        UpdateTexts();
        slider.value = latencyMs;
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void Update(){
        clocksMs += Time.deltaTime * 1000;

        if (Keyboard.current.dKey.wasPressedThisFrame && !measuring){ 
            pressFrame = Time.frameCount;
            pressTime = Time.time;
            measuring = true;
        }
            
        while (clocksMs >= nextTickMs){
            Vector2 dirA = ReadInputsWASD();
            Vector2 dirB = ReadInputsArrows();

            clientA.Tick(nextTickMs, dirA);
            clientB.Tick(nextTickMs, dirB);
            server.Tick(nextTickMs, tickMs / 1000f);
            nextTickMs += tickMs;
        } 

        cubeA.position = clientA.State.GetPosition(0);
        cubeB.position = clientB.State.GetPosition(1);

        if (measuring && cubeA.position.x != lastX){ 
            Debug.Log("Delay Frame:" + (Time.frameCount - pressFrame)); 
            Debug.Log("Delay ms:" + (Time.time - pressTime) * 1000); 
            measuring = false;
        } 
        lastX = cubeA.position.x;
        
        UpdateTexts();
    }

    private void UpdateTexts(){
        clientAPositionText.text = $"Client A:  {clientA.State.GetPosition(0).ToString("F2")}";
        clientBPositionText.text = $"Client B:  {clientB.State.GetPosition(1).ToString("F2")}";
        serverPositionText.text = $"Server:  {server.State.GetPosition(0).ToString("F2")}";
    }

    private Vector2 ReadInputsWASD(){
        Vector2 direction = Vector2.zero;
        if (Keyboard.current == null) return Vector2.zero;
        if (Keyboard.current.wKey.isPressed) direction.y += 1;
        if (Keyboard.current.sKey.isPressed) direction.y -= 1;
        if (Keyboard.current.dKey.isPressed)direction.x += 1;
        if (Keyboard.current.aKey.isPressed) direction.x -= 1;

        return direction;
    }

    private Vector2 ReadInputsArrows(){
        Vector2 direction = Vector2.zero;

        if (Keyboard.current == null) return Vector2.zero;
        if (Keyboard.current.upArrowKey.isPressed) direction.y += 1;
        if (Keyboard.current.downArrowKey.isPressed) direction.y -= 1;
        if (Keyboard.current.rightArrowKey.isPressed) direction.x += 1;
        if (Keyboard.current.leftArrowKey.isPressed) direction.x -= 1;


        return direction;
    }

    void OnSliderChanged(float newValue){
        latencyMs =  Mathf.RoundToInt(newValue); 
        RTT = latencyMs * 2; 
        currentLatency.text = $"Latency: {latencyMs}"; 
        rttText.text = $"RTT: {RTT}";
        SetLatency();
    }

    private void SetLatency(){
        for (int i = 0; i < Players; i++){
            inputQueues[i].latency = latencyMs;
        }

        for (int i = 0; i < Players; i++){
            outputQueues[i].latency = latencyMs;
        }
    }

    private void OnDestroy(){
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }
}