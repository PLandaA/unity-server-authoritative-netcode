//Time in whole milliseconds. This wire preserves order; real UDP does not guarantee it.

using System.Collections.Generic;

public class LagQueue<T> 
{   
    public int Latency;
    
    private Queue<(int deliveryTime, T message)> queue = new();


    public LagQueue(int latency){
        Latency = latency;
    }

    public void Send(T message, int currentTime){ 
        queue.Enqueue((currentTime + Latency, message)); 
            
        
    }

    
    public bool TryReceive(int currentTime, out T message){
        if (queue.Count == 0){
            message = default; 
            return false;
        }

        if (queue.Peek().deliveryTime > currentTime){
            message = default; 
            return false;
        } 
        
        message = queue.Dequeue().message; 
        return true;
        
    }


}

