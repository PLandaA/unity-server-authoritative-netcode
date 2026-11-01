
using NUnit.Framework;

public class LagQueueTest 
{
   private LagQueue<string> lagQueue;  

   [SetUp]
   public void SetUp(){
     lagQueue = new LagQueue<string>(100);  
      
   }
   
   [Test]
   public void TryReceive_DeliversExactlyOnTime(){ 
      lagQueue.Send("hello", 0);
      Assert.IsFalse(lagQueue.TryReceive(99, out string message)); 
      Assert.IsTrue(lagQueue.TryReceive(100, out message));  
      Assert.AreEqual("hello", message); 
      Assert.IsFalse(lagQueue.TryReceive(100, out message));  
      
   }

   [Test]
   public void TryReceive_ReturnsInArrivalOrder(){  
      lagQueue.Send("a", 0);  
      lagQueue.Send("b", 50); 
      Assert.IsTrue(lagQueue.TryReceive(100, out string message)); 
      Assert.AreEqual("a", message);      
      Assert.IsFalse(lagQueue.TryReceive(100, out message)); 
      Assert.IsTrue(lagQueue.TryReceive(150, out message));
      Assert.AreEqual("b", message);
   }
}
