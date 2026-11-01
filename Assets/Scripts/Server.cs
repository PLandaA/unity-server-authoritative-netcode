using UnityEngine;

public class Server{
    private StateMessage state;

    public StateMessage State => state;

    private readonly LagQueue<InputMessage>[] inputQueues;
    private readonly LagQueue<StateMessage>[] outputQueues;
    private readonly float movementVel;
    private readonly Vector2[] tickDirections;
    private readonly int[] lastProcessedSequence;
    private int serverTick;


    public Server(LagQueue<InputMessage>[] inputQueues, LagQueue<StateMessage>[] outputQueues, float movementVel){
        this.inputQueues = inputQueues;
        this.outputQueues = outputQueues;
        this.movementVel = movementVel;
        tickDirections = new Vector2[inputQueues.Length];
        lastProcessedSequence = new int[inputQueues.Length];
        System.Array.Fill(lastProcessedSequence, -1);
    }


    public void Tick(int currentTime, float dt){
        Drain(currentTime);
        Simulate(dt);
        Publish(currentTime);
    }

    private void Drain(int currentTime){
        System.Array.Clear(tickDirections, 0, tickDirections.Length);
        for (int i = 0; i < inputQueues.Length; i++){
            while (inputQueues[i].TryReceive(currentTime, out var input)){
                if (input.ClientId != i){
                    Debug.LogWarning($"Input with clientId {input.ClientId} received on queue {i}; ignored");
                    continue;
                }

                if (input.Sequence <= lastProcessedSequence[i]){
                    continue;
                }

                tickDirections[i] = input.Direction;
                lastProcessedSequence[i] = input.Sequence;
            }
        }
    }

    private void Simulate(float dt){
        for (int i = 0; i < inputQueues.Length; i++){
            Vector2 newPosition =  Movement.GetNewPosition(state.GetPosition(i), tickDirections[i], movementVel, dt);
            state.SetPosition(i, newPosition); 
        }
    }


    private void Publish(int currentTime){
        for (int i = 0; i < lastProcessedSequence.Length; i++){
            state.SetLastProcessedSequence(i, lastProcessedSequence[i]);
        }
        state.ServerTick = serverTick;

        foreach (var outputQueue in outputQueues){
            outputQueue.Send(state, currentTime);
        }

        serverTick++;
    }
}