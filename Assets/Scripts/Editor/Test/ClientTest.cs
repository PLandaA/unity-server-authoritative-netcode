using NUnit.Framework;
using UnityEngine;

public class ClientTest{
    private Client client;
    private LagQueue<InputMessage> inputQueue;
    private LagQueue<StateMessage> outputQueue;


    [SetUp]
    public void Setup(){
        inputQueue = new LagQueue<InputMessage>(0);
        outputQueue = new LagQueue<StateMessage>(0);
        client = new Client(0, inputQueue, outputQueue, 1, 0.05f);
        client.PredictionEnabled = true;
    }


    [Test]
    public void Tick_WithDirection_SendsInputWithItsClientId(){
        client.Tick(0, Vector2.right);
        InputMessage inputMessage = new InputMessage{ ClientId = 0, Direction = Vector2.right };
        Assert.IsTrue(inputQueue.TryReceive(0, out var send));
        Assert.AreEqual(inputMessage, send);
    }


    [Test]
    public void Tick_WithSnapshot_UpdatesPosition(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(3, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        Assert.AreEqual(new Vector2(3, 0), client.State.GetPosition(0));
    }

    [Test]
    public void Tick_WithoutSnapshot_KeepsPosition(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(3, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        client.Tick(0, Vector2.zero);
        Assert.AreEqual(new Vector2(3, 0), client.State.GetPosition(0));
    }

    [Test]
    public void Tick_ThreeTimes_NumbersInputsFromZero(){
        client.Tick(0, Vector2.zero);
        client.Tick(0, Vector2.zero);
        client.Tick(0, Vector2.zero);
        Assert.IsTrue(inputQueue.TryReceive(0, out InputMessage input));
        Assert.AreEqual(0, input.Sequence);
        Assert.IsTrue(inputQueue.TryReceive(0, out input));
        Assert.AreEqual(1, input.Sequence);
        Assert.IsTrue(inputQueue.TryReceive(0, out input));
        Assert.AreEqual(2, input.Sequence);
    }

    [Test]
    public void Reconcile_StaleSnapshot_DoesNotJump(){
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(0.05f, 0);
        stateMessage.SetLastProcessedSequence(0, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.right);
        Assert.AreEqual(0.25f, client.DisplayPosition.x, 0.0001f);
    }

    [Test]
    public void Reconcile_ServerDiffers_CorrectsAndReapplies(){
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(0.50f, 0);
        stateMessage.SetLastProcessedSequence(0, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.right);
        Assert.AreEqual(0.70f, client.DisplayPosition.x, 0.0001f);
    }

    [Test]
    public void Reconcile_ConfirmsAll_EmptiesBuffer(){
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(0.15f, 0);
        stateMessage.SetLastProcessedSequence(0, 2);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        Assert.AreEqual(1, client.PendingInputCount);
    }

    [Test]
    public void Tick_WithoutPrediction_DoesNotAccumulateBuffer(){
        client.PredictionEnabled = false;
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        Assert.AreEqual(0, client.PendingInputCount);
    }

    [Test]
    public void Reconcile_Disabled_SnapsToServer(){
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.Tick(0, Vector2.right);
        client.ReconciliationEnabled = false;
        StateMessage stateMessage = new StateMessage();
        stateMessage.P0 = new Vector2(0.05f, 0);
        stateMessage.SetLastProcessedSequence(0, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.right);
        Assert.AreEqual(0.05f, client.DisplayPosition.x);
        Assert.AreEqual(0, client.PendingInputCount);
    }

    [Test]
    public void GetRemotePosition_Interpolates(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.P1 = new Vector2(0, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        stateMessage.P1 = new Vector2(1, 0);
        outputQueue.Send(stateMessage, 50);
        client.Tick(50, Vector2.zero);
        Assert.AreEqual(.5f, client.GetRemotePosition(125).x);
    }


    [Test]
    public void GetRemotePosition_Disabled_ReturnsLatest(){
        StateMessage stateMessage = new StateMessage();
        stateMessage.P1 = new Vector2(0, 0);
        outputQueue.Send(stateMessage, 0);
        client.Tick(0, Vector2.zero);
        stateMessage.P1 = new Vector2(1, 0);
        outputQueue.Send(stateMessage, 50);
        client.Tick(50, Vector2.zero);
        client.InterpolationEnabled = false;
        Assert.AreEqual(1, client.GetRemotePosition(125).x);
    }
}