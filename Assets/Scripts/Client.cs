using System.Collections.Generic;
using UnityEngine;

public class Client{
    private StateMessage state;
    public StateMessage State => state;
    public Vector2 DisplayPosition => predictedPosition;
    public int PendingInputCount => pendingInputsBuffer.Count;
    public bool PredictionEnabled = true;
    public bool ReconciliationEnabled = true;
    public int InterpolationDelayMs = 100;
    public bool InterpolationEnabled = true;


    private readonly LagQueue<InputMessage> inputQueue;
    private readonly LagQueue<StateMessage> outputQueue;
    private readonly SnapshotInterpolator remoteInterpolator = new();
    private readonly int clientId;
    private int nextSequence;
    readonly int otherId;


    private readonly float speed;
    private readonly float dt;

    private Vector2 predictedPosition;
    private List<InputMessage> pendingInputsBuffer = new List<InputMessage>();


    public Client(int clientId, LagQueue<InputMessage> inputQueue, LagQueue<StateMessage> outputQueue, float speed,
        float dt, Vector2[] startPositions = null){
        this.clientId = clientId;
        this.inputQueue = inputQueue;
        this.outputQueue = outputQueue;
        this.speed = speed;
        this.dt = dt;
        otherId = 1 - clientId; // Only works for 2 players

        if (startPositions != null){
            for (int i = 0; i < startPositions.Length; i++){
                state.SetPosition(i, startPositions[i]);
            }
            predictedPosition = startPositions[clientId];
            remoteInterpolator.Add(0, startPositions[otherId]);
        }
    }

    public void Tick(int currentTime, Vector2 inputDirection){
        InputMessage inputMessage = new InputMessage
            { ClientId = clientId, Direction = inputDirection, Sequence = nextSequence };
        inputQueue.Send(inputMessage, currentTime);
        nextSequence++;
        if (PredictionEnabled){
            predictedPosition = Movement.GetNewPosition(predictedPosition, inputDirection, speed, dt);
            pendingInputsBuffer.Add(inputMessage);
        }

        bool snapshotArrived = false;
        while (outputQueue.TryReceive(currentTime, out var snapshot)){
            state = snapshot;
            snapshotArrived = true;
            remoteInterpolator.Add(currentTime, snapshot.GetPosition(otherId));
        }

        if (snapshotArrived){
            predictedPosition = state.GetPosition(clientId);
            if (ReconciliationEnabled){
                pendingInputsBuffer.RemoveAll(input => input.Sequence <= state.GetLastProcessedSequence(clientId)
                );
                foreach (var pendingInput in pendingInputsBuffer){
                    predictedPosition = Movement.GetNewPosition(predictedPosition, pendingInput.Direction, speed, dt);
                }
            }
            else{
                pendingInputsBuffer.Clear();
            }
        }
    }

    public Vector2 GetRemotePosition(float nowMs){
        if (InterpolationEnabled){
            return remoteInterpolator.Sample(nowMs - InterpolationDelayMs);
        }
        else{
            return state.GetPosition(otherId);
        }
    }
}