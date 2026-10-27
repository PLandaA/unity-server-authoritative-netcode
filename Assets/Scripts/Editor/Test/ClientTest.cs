using NUnit.Framework;
using UnityEngine;

public class ClientTest{
    Client client;
    LagQueue<InputMessage> inputQueue;
    LagQueue<StateMessage> outputQueue;


    [SetUp]
    public void Setup(){
        inputQueue = new LagQueue<InputMessage>(0);
        outputQueue = new LagQueue<StateMessage>(0);
        client = new Client(0, inputQueue, outputQueue);
    }


    [Test]
    public void Tick_ConDireccion_EnviaInputConSuClientId(){
        client.Tick(0, Vector2.right);
        InputMessage inputMessage = new InputMessage{ clientId = 0, direction = Vector2.right };
        Assert.IsTrue(inputQueue.TryReceive(0, out var send));
        Assert.AreEqual(inputMessage, send);
    }


    [Test]
    public void Tick_ConSnapshot_ActualizaPosicion(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.p0 = new Vector2(3, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        Assert.AreEqual(new Vector2(3, 0), client.State.GetPosition(0));
    }

    [Test]
    public void Tick_SinSnapshot_ConservaPosicion(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.p0 = new Vector2(3, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        client.Tick(0, Vector2.zero);
        Assert.AreEqual( new Vector2(3, 0), client.State.GetPosition(0));
    }
}