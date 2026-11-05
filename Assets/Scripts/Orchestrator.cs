using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Orchestrator : MonoBehaviour{
    [SerializeField] private Slider latencySlider;
    [SerializeField] private Slider interpolationSlider;

    [SerializeField] private Toggle clientPredictionToggle;

    [SerializeField] private Toggle serverReconciliationToggle;
    [SerializeField] private Toggle interpolationToggle;
    [SerializeField] private Transform cubeA;
    [SerializeField] private Transform cubeB;
    [SerializeField] private Transform cubeBSeenByA;
    [SerializeField] private Transform cubeASeenByB;
    [SerializeField] private float speed;
    [SerializeField] private int tickMs = 50;


    [SerializeField] private TMP_Text currentLatency;
    [SerializeField] private TMP_Text currentInterpolation;
    [SerializeField] private TMP_Text rttText;
    [SerializeField] private TMP_Text serverPositionText;

    [SerializeField] private TMP_Text clientAServerSaidPositionText;

    [SerializeField] private TMP_Text clientAPredictedPositionText;

    [SerializeField] private TMP_Text clientAPendingInputCountText;

    [SerializeField] private TMP_Text clientBServerSaidPositionText;

    [SerializeField] private TMP_Text clientBPredictedPositionText;
    [SerializeField] private TMP_Text clientBPendingInputCountText;

    [SerializeField] private TMP_Text bSeenByAText;
    [SerializeField] private TMP_Text serverBForAText;
    [SerializeField] private TMP_Text aSeenByBText;
    [SerializeField] private TMP_Text serverAForBText;


    private LagQueue<InputMessage>[] inputQueues;
    private LagQueue<StateMessage>[] outputQueues;

    private const int Players = 2;
    private static readonly Vector2[] StartPositions = { new Vector2(-2f, 0f), new Vector2(2f, 0f) };
    private int latencyMs = 100;
    private int interpolationDelayMs = 100;
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
        server = new Server(inputQueues, outputQueues, speed, StartPositions);
        clientA = new Client(0, inputQueues[0], outputQueues[0], speed, dt, StartPositions);
        clientB = new Client(1, inputQueues[1], outputQueues[1], speed, dt, StartPositions);
    }

    private void Start(){
        SetupLatencySlider();
        SetupInterpolationSlider();
        SetupClientPrediction();
        SetupServerReconciliation();
        SetupInterpolation();
        UpdatePositionTexts();
    }

    private void SetupLatencySlider(){
        latencySlider.SetValueWithoutNotify(latencyMs);
        latencySlider.onValueChanged.AddListener(OnLatencySliderChanged);
        OnLatencySliderChanged(latencyMs);
    }
    private void SetupInterpolationSlider(){
        interpolationSlider.SetValueWithoutNotify(interpolationDelayMs);
        interpolationSlider.onValueChanged.AddListener(OnInterpolationSliderChanged);
        OnInterpolationSliderChanged(interpolationDelayMs);
    }

    private void SetupClientPrediction(){
        clientPredictionToggle.onValueChanged.AddListener(OnClientPredictionToggleChanged);
        OnClientPredictionToggleChanged(clientPredictionToggle.isOn);
    }

    private void SetupServerReconciliation(){
        serverReconciliationToggle.onValueChanged.AddListener(OnServerReconciliationToggleChanged);
        OnServerReconciliationToggleChanged(serverReconciliationToggle.isOn);
    }

    private void SetupInterpolation(){
        interpolationToggle.onValueChanged.AddListener(OnInterpolationToggleChanged);
        OnInterpolationToggleChanged(interpolationToggle.isOn);
    }

    private void Update(){
        clockMs += Time.deltaTime * 1000;

        ProcessTicks();

        cubeA.position = clientA.DisplayPosition;
        cubeB.position = clientB.DisplayPosition;
        cubeBSeenByA.position = clientA.GetRemotePosition(clockMs);
        cubeASeenByB.position = clientB.GetRemotePosition(clockMs);

        //MeasureInputDelay(); Uncomment to debug Input Delay
        UpdatePositionTexts();
    }

    private void ProcessTicks(){
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
        UpdateClientTexts(clientA, 0, "A", "B", clockMs,
            clientAServerSaidPositionText, clientAPredictedPositionText, clientAPendingInputCountText,
            bSeenByAText, serverBForAText);
        UpdateClientTexts(clientB, 1, "B", "A", clockMs,
            clientBServerSaidPositionText, clientBPredictedPositionText, clientBPendingInputCountText,
            aSeenByBText, serverAForBText);
    }

    private void UpdateClientTexts(Client client, int clientId, string myName, string otherName, float nowMs,
        TMP_Text serverSaidText, TMP_Text predictedText, TMP_Text pendingText,
        TMP_Text otherSeenText, TMP_Text otherServerText){
        int otherId = 1 - clientId;
        serverSaidText.text = $"Server-said:  {client.State.GetPosition(clientId).ToString("F2")}";
        predictedText.text = $"Predicted:  {client.DisplayPosition.ToString("F2")}";
        pendingText.text = $"Pending Input Count:  {client.PendingInputCount}";
        otherSeenText.text = $"{otherName} seen by {myName}:  {client.GetRemotePosition(nowMs).ToString("F2")}";
        otherServerText.text = $"Server {otherName}:  {server.State.GetPosition(otherId).ToString("F2")}";
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

    private void OnLatencySliderChanged(float newValue){
        latencyMs = Mathf.RoundToInt(newValue);
        rtt = latencyMs * 2;
        UpdateNetworkTexts();
        SetLatency(inputQueues, latencyMs);
        SetLatency(outputQueues, latencyMs);
    } 
    
    private void OnInterpolationSliderChanged(float newValue){
        interpolationDelayMs = Mathf.RoundToInt(newValue);
        clientA.InterpolationDelayMs = interpolationDelayMs;
        clientB.InterpolationDelayMs = interpolationDelayMs;
        currentInterpolation.text = $"Interp delay: {interpolationDelayMs}";
    }

    private void OnClientPredictionToggleChanged(bool newValue){
        clientA.PredictionEnabled = newValue;
        clientB.PredictionEnabled = newValue;
    }

    private void OnServerReconciliationToggleChanged(bool newValue){
        clientA.ReconciliationEnabled = newValue;
        clientB.ReconciliationEnabled = newValue;
    }

    private void OnInterpolationToggleChanged(bool newValue){
        clientA.InterpolationEnabled = newValue; 
        clientB.InterpolationEnabled = newValue; 
    }

    private void OnDestroy(){
        latencySlider.onValueChanged.RemoveListener(OnLatencySliderChanged);
        interpolationSlider.onValueChanged.RemoveListener(OnInterpolationSliderChanged);
        clientPredictionToggle.onValueChanged.RemoveListener(OnClientPredictionToggleChanged);
        serverReconciliationToggle.onValueChanged.RemoveListener(OnServerReconciliationToggleChanged);
        interpolationToggle.onValueChanged.RemoveListener(OnInterpolationToggleChanged);
    }
}