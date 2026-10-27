
using NUnit.Framework;

public class LagQueueTest 
{
   LagQueue<string> lagQueue;  

   [SetUp]
   public void SetUp(){
     lagQueue = new LagQueue<string>(100);  
      
   }
   
   [Test]
   public void TryReceive_EntregaJustoEnLaHora(){ 
      lagQueue.Send("hola", 0);
      Assert.IsFalse(lagQueue.TryReceive(99, out string mensaje)); 
      Assert.IsTrue(lagQueue.TryReceive(100, out mensaje));  
      Assert.AreEqual("hola", mensaje); 
      Assert.IsFalse(lagQueue.TryReceive(100, out mensaje));  
      
   }

   [Test]
   public void TryReceive_SalenEnOrdenDeEntrada(){  
      lagQueue.Send("a", 0);  
      lagQueue.Send("b", 50); 
      Assert.IsTrue(lagQueue.TryReceive(100, out string mensaje)); 
      Assert.AreEqual("a", mensaje);      
      Assert.IsFalse(lagQueue.TryReceive(100, out mensaje)); 
      Assert.IsTrue(lagQueue.TryReceive(150, out mensaje));
      Assert.AreEqual("b", mensaje);
   }
}
