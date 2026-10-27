using UnityEngine;

public class Client{
    StateMessage state;
    public StateMessage State => state;


    LagQueue<InputMessage> _inputQueue;
    LagQueue<StateMessage> _outputQueue;
    private int _clientId;
    private int nextSequence;


    public Client(int clientId, LagQueue<InputMessage> inputQueue, LagQueue<StateMessage> outputQueue){
        this._clientId = clientId;
        this._inputQueue = inputQueue;
        this._outputQueue = outputQueue;
    }

    public void Tick(int currentTime, Vector2 inputDirection){
        InputMessage inputMessage = new InputMessage{ clientId = _clientId, direction = inputDirection, sequence = nextSequence };
        _inputQueue.Send(inputMessage, currentTime);
        nextSequence++;
        while (_outputQueue.TryReceive(currentTime, out var snapshot)){
            state = snapshot;
        }
    }
}