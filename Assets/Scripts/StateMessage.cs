using UnityEngine;

public struct StateMessage{
    public Vector2 p0, p1;
    public int lastProcessedSequenceClient0;
    public int lastProcessedSequenceClient1;

    public int serverTick;

    public Vector2 GetPosition(int clientId){
        if (clientId < 0 || clientId > 1){
            return Vector2.zero;
        }

        return clientId == 0 ? p0 : p1;
    }

    public void SetPosition(int clientId, Vector2 position){
        if (clientId == 0){
            p0 = position;
        }
        else if (clientId == 1){
            p1 = position;
        }
    }

    public int GetLastProcessedSequence(int clientId){
        if (clientId < 0 || clientId > 1){
            return -1;
        }

        return clientId == 0 ? lastProcessedSequenceClient0 : lastProcessedSequenceClient1;
    }

    public void SetLastProcessedSequence(int clientId, int value){
        if (clientId == 0){
            lastProcessedSequenceClient0 = value;
        }
        else if (clientId == 1){
            lastProcessedSequenceClient1 = value;
        }
    }
}