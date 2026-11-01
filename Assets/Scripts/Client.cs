using System.Collections.Generic;
using UnityEngine;

public class Client{
    private StateMessage state;
    public StateMessage State => state;
    public Vector2 DisplayPosition => predictedPosition;
    public int PendingInputCount => pendingInputsBuffer.Count;
    public bool PredictionEnabled = true;
    public bool ReconciliationEnabled = true;


    private readonly LagQueue<InputMessage> inputQueue;
    private readonly LagQueue<StateMessage> outputQueue;
    private readonly int clientId;
    private int nextSequence;

    private readonly float speed;
    private readonly float dt;

    private Vector2 predictedPosition;
    private List<InputMessage> pendingInputsBuffer = new List<InputMessage>();


    public Client(int clientId, LagQueue<InputMessage> inputQueue, LagQueue<StateMessage> outputQueue, float speed,
        float dt){
        this.clientId = clientId;
        this.inputQueue = inputQueue;
        this.outputQueue = outputQueue;
        this.speed = speed;
        this.dt = dt;
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
}