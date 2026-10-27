//tiempo en milisegundos enteros, el cable preserva orden, UDP real no lo garantiza.

using System.Collections.Generic;

public class LagQueue<T> 
{   
    public int latency;
    
    Queue<(int tiempoDeEntrega, T mensaje)> queue = new();


    public LagQueue(int latency){
        this.latency = latency;
    }

    public void Send(T mensaje, int tiempoActual){ 
        queue.Enqueue((tiempoActual + latency, mensaje)); 
            
        
    }

    
    public bool TryReceive(int tiempoActual, out T mensaje){
        if (queue.Count == 0){
            mensaje = default; 
            return false;
        }

        if (queue.Peek().tiempoDeEntrega > tiempoActual){
            mensaje = default; 
            return false;
        } 
        
        mensaje = queue.Dequeue().mensaje; 
        return true;
        
    }


}

