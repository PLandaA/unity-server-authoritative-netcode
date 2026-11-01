using UnityEngine;

public static class Movement {

    public static Vector2 GetNewPosition(Vector2 position, Vector2 direction, float velocity, float dt){
        Vector2 newPosition = position + direction * (velocity * dt);
        return newPosition;

    } 
}
