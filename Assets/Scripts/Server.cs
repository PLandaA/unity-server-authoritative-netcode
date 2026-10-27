using UnityEngine;

public class Server{
    StateMessage state;

    public StateMessage State => state;

    LagQueue<InputMessage>[] _inputQueues;
    LagQueue<StateMessage>[] _outputQueues;
    float movementVel;
    Vector2[] tickDirections;
    int[] lastProcessedSequence;
    private int serverTick;


    public Server(LagQueue<InputMessage>[] inputQueues, LagQueue<StateMessage>[] outputQueues, float movementVel){
        this._inputQueues = inputQueues;
        this._outputQueues = outputQueues;
        this.movementVel = movementVel;
        tickDirections = new Vector2[_inputQueues.Length];
        lastProcessedSequence = new int[_inputQueues.Length];
        System.Array.Fill(lastProcessedSequence, -1);
    }


    public void Tick(int currentTime, float dt){
        Drain(currentTime);
        Simulate(dt);
        Publish(currentTime);
    }

    private void Drain(int currentTime){
        System.Array.Clear(tickDirections, 0, tickDirections.Length);
        for (int i = 0; i < _inputQueues.Length; i++){
            while (_inputQueues[i].TryReceive(currentTime, out var input)){
                if (input.clientId != i){
                    Debug.LogWarning($"Input con clientId {input.clientId} recibido por la cola {i}; ignorado");
                    continue;
                }

                if (input.sequence <= lastProcessedSequence[i]){
                    continue;
                }

                tickDirections[i] = input.direction;
                lastProcessedSequence[i] = input.sequence;
            }
        }
    }

    private void Simulate(float dt){
        for (int i = 0; i < _inputQueues.Length; i++){
            Vector2 newPosition = state.GetPosition(i) + tickDirections[i] * movementVel * dt;
            state.SetPosition(i, newPosition);
        }
    }


    private void Publish(int currentTime){
        for (int i = 0; i < lastProcessedSequence.Length; i++){
            state.SetLastProcessedSequence(i, lastProcessedSequence[i]);
        }
        state.serverTick = serverTick;

        for (int i = 0; i < _outputQueues.Length; i++){
            _outputQueues[i].Send(state, currentTime);
        }

        serverTick++;
    }
}