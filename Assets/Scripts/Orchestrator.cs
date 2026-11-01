using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Orchestrator : MonoBehaviour{
    [SerializeField] private Slider slider;

    [FormerlySerializedAs("toggle")] [SerializeField]
    private Toggle clientPredictionToggle;

    [SerializeField] private Toggle serverReconciliationToggle;
    [SerializeField] private Transform cubeA;
    [SerializeField] private Transform cubeB;
    [SerializeField] private float speed;
    [SerializeField] private int tickMs = 50;


    [SerializeField] private TMP_Text currentLatency;
    [SerializeField] private TMP_Text rttText;
    [SerializeField] private TMP_Text serverPositionText;

    [FormerlySerializedAs("clientAPositionText")] [SerializeField]
    private TMP_Text clientAServerSaidPositionText;

    [SerializeField] private TMP_Text clientAPredictedPositionText;

    [FormerlySerializedAs("pendingInputCountText")] [SerializeField]
    private TMP_Text clientAPendingInputCountText;

    [FormerlySerializedAs("clientBPositionText")] [SerializeField]
    private TMP_Text clientBServerSaidPositionText;

    [SerializeField] private TMP_Text clientBPredictedPositionText;
    [SerializeField] private TMP_Text clientBPendingInputCountText;


    private LagQueue<InputMessage>[] inputQueues;
    private LagQueue<StateMessage>[] outputQueues;

    private const int Players = 2;
    private int latencyMs = 100;
    private int rtt;
    private float clockMs;
    private int nextTickMs;

    private Server server;
    private Client clientA;
    private Client clientB;


    private bool measuring;
    private int pressFrame;
    private float pressTime;
    private float lastX;
    private float dt;

    private void Awake(){
        if (tickMs <= 0){
            Debug.LogError("Tick ms must be greater than zero");
            tickMs = 50;
        }

        inputQueues = CreateQueues<InputMessage>(Players, latencyMs);
        outputQueues = CreateQueues<StateMessage>(Players, latencyMs);

        dt = tickMs / 1000f;
        server = new Server(inputQueues, outputQueues, speed);
        clientA = new Client(0, inputQueues[0], outputQueues[0], speed, dt);
        clientB = new Client(1, inputQueues[1], outputQueues[1], speed, dt);
    }

    private void Start(){
        SetupLatencySlider();
        SetupClientPrediction();
        SetupServerReconciliation();
        UpdatePositionTexts();
    }

    private void SetupLatencySlider(){
        slider.SetValueWithoutNotify(latencyMs);
        slider.onValueChanged.AddListener(OnSliderChanged);
        OnSliderChanged(latencyMs);
    }

    private void SetupClientPrediction(){
        clientPredictionToggle.onValueChanged.AddListener(OnClientPredictionToggleChanged);
        OnClientPredictionToggleChanged(clientPredictionToggle.isOn);
    }

    private void SetupServerReconciliation(){
        serverReconciliationToggle.onValueChanged.AddListener(OnServerReconciliationToggleChanged);
        OnServerReconciliationToggleChanged(serverReconciliationToggle.isOn);
    }

    private void Update(){
        clockMs += Time.deltaTime * 1000;

        ProcessTicks();

        cubeA.position = clientA.DisplayPosition;
        cubeB.position = clientB.DisplayPosition;

        MeasureInputDelay();
        UpdatePositionTexts();
    }

    private void ProcessTicks(){
        dt = tickMs / 1000f;
        while (clockMs >= nextTickMs){
            Vector2 dirA = ReadDirection(Key.W, Key.S, Key.D, Key.A);
            Vector2 dirB = ReadDirection(Key.UpArrow, Key.DownArrow, Key.RightArrow, Key.LeftArrow);

            clientA.Tick(nextTickMs, dirA);
            clientB.Tick(nextTickMs, dirB);
            server.Tick(nextTickMs, dt);
            nextTickMs += tickMs;
        }
    }

    //Debug instrumentation: logs how long it takes for cube A to react after pressing D.
    private void MeasureInputDelay(){
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.dKey.wasPressedThisFrame && !measuring){
            pressFrame = Time.frameCount;
            pressTime = Time.time;
            measuring = true;
        }

        if (measuring && cubeA.position.x != lastX){
            Debug.Log("Delay Frame:" + (Time.frameCount - pressFrame));
            Debug.Log("Delay ms:" + (Time.time - pressTime) * 1000);
            measuring = false;
        }

        lastX = cubeA.position.x;
    }

    private void UpdatePositionTexts(){
        serverPositionText.text =
            $"Server:  A {server.State.GetPosition(0).ToString("F2")}  B {server.State.GetPosition(1).ToString("F2")}";
        UpdateClientTexts(clientA, 0, clientAServerSaidPositionText, clientAPredictedPositionText,
            clientAPendingInputCountText);
        UpdateClientTexts(clientB, 1, clientBServerSaidPositionText, clientBPredictedPositionText,
            clientBPendingInputCountText);
    }

    private static void UpdateClientTexts(Client client, int clientId, TMP_Text serverSaidText, TMP_Text predictedText,
        TMP_Text pendingText){
        serverSaidText.text = $"Server-said:  {client.State.GetPosition(clientId).ToString("F2")}";
        predictedText.text = $"Predicted:  {client.DisplayPosition.ToString("F2")}";
        pendingText.text = $"Pending Input Count:  {client.PendingInputCount}";
    }

    private void UpdateNetworkTexts(){
        currentLatency.text = $"Latency: {latencyMs}";
        rttText.text = $"RTT: {rtt}";
    }

    private static Vector2 ReadDirection(Key up, Key down, Key right, Key left){
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        Vector2 direction = Vector2.zero;
        if (keyboard[up].isPressed) direction.y += 1;
        if (keyboard[down].isPressed) direction.y -= 1;
        if (keyboard[right].isPressed) direction.x += 1;
        if (keyboard[left].isPressed) direction.x -= 1;
        return direction;
    }

    private static LagQueue<T>[] CreateQueues<T>(int count, int latency){
        LagQueue<T>[] queues = new LagQueue<T>[count];
        for (int i = 0; i < count; i++){
            queues[i] = new LagQueue<T>(latency);
        }

        return queues;
    }

    private static void SetLatency<T>(LagQueue<T>[] queues, int latency){
        foreach (var queue in queues){
            queue.Latency = latency;
        }
    }

    private void OnSliderChanged(float newValue){
        latencyMs = Mathf.RoundToInt(newValue);
        rtt = latencyMs * 2;
        UpdateNetworkTexts();
        SetLatency(inputQueues, latencyMs);
        SetLatency(outputQueues, latencyMs);
    }

    private void OnClientPredictionToggleChanged(bool newValue){
        clientA.PredictionEnabled = newValue;
        clientB.PredictionEnabled = newValue;
    }

    private void OnServerReconciliationToggleChanged(bool newValue){
        clientA.ReconciliationEnabled = newValue;
        clientB.ReconciliationEnabled = newValue;
    }

    private void OnDestroy(){
        slider.onValueChanged.RemoveListener(OnSliderChanged);
        clientPredictionToggle.onValueChanged.RemoveListener(OnClientPredictionToggleChanged);
        serverReconciliationToggle.onValueChanged.RemoveListener(OnServerReconciliationToggleChanged);
    }
}