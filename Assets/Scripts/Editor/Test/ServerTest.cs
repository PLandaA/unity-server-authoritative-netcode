using NUnit.Framework;
using UnityEngine; 

public class ServerTest 
{
    LagQueue<InputMessage>[] inputQueues;  
    LagQueue<StateMessage>[] outputQueues;

    private const int Players = 2;
    private Server server;

    [SetUp]
    public void SetUp(){
        inputQueues = new LagQueue<InputMessage>[Players];
        for (int i = 0; i < Players; i++){ 
            inputQueues[i] = new LagQueue<InputMessage>(0);
        }
     
        outputQueues = new LagQueue<StateMessage>[Players];
        for (int i = 0; i < Players; i++){ 
            outputQueues[i] = new LagQueue<StateMessage>(0);
        } 
        server = new Server(inputQueues, outputQueues, 1f);
    }

    [Test]
    public void Tick_DosInputsDerecha_MueveSoloAlCliente0(){ 
        InputMessage inputMessage = new InputMessage { clientId = 0, direction = Vector2.right };  
        
        inputQueues[0].Send(inputMessage, 0); 
        server.Tick(0,0.05f); 

        inputQueues[0].Send(inputMessage, 50); 
        server.Tick(50,0.05f); 
        
        Assert.AreEqual(0.1f, server.State.GetPosition(0).x, 0.0001f); 
        Assert.That(server.State.GetPosition(1), Is.EqualTo(Vector2.zero) );
    } 
    
    [Test]
    public void Tick_SinInput_ElJugadorNoSeMueve(){ 
        InputMessage inputMessage = new InputMessage { clientId = 0, direction = Vector2.right };  
        
        inputQueues[0].Send(inputMessage, 0); 
        server.Tick(0,0.05f); 

        inputQueues[0].Send(inputMessage, 50); 
        server.Tick(50,0.05f);  
        
        server.Tick(100,0.05f); 
        
        Assert.AreEqual(0.1f, server.State.GetPosition(0).x, 0.0001f); 
        Assert.AreEqual(Vector2.zero, server.State.GetPosition(1)); 
    }  
    
    [Test]
    public void Tick_MismaPosicionDelServidorConElCliente(){ 
        InputMessage inputMessage = new InputMessage { clientId = 0, direction = Vector2.right };  
        
        inputQueues[0].Send(inputMessage, 0); 
        server.Tick(0,0.05f);

        Assert.IsTrue(outputQueues[0].TryReceive(0, out var snapshot)); 
        Assert.AreEqual(server.State.GetPosition(0), snapshot.GetPosition(0)); 
        Assert.IsTrue(outputQueues[1].TryReceive(0, out snapshot));  
        Assert.AreEqual(server.State.GetPosition(0), snapshot.GetPosition(0)); 
        Assert.AreEqual(server.State.GetPosition(1), snapshot.GetPosition(1)); 
    } 
}