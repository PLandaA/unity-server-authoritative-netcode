using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;


public class MovementTest{

    [Test]
    public void GetNewPosition_Right_AdvancesVelocityTimesDt(){
        Assert.That(new Vector2(0.05f, 0), Is.EqualTo(Movement.GetNewPosition(Vector2.zero, Vector2.right, 1, 0.05f)).Using(Vector2EqualityComparer.Instance));
        

    }
    
}
