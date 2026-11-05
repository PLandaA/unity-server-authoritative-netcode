using System.Collections.Generic;
using UnityEngine;

public class SnapshotInterpolator{
    public int Count => samples.Count;

    private List<(float timeMs, Vector2 position)> samples = new();


    public void Add(float timeMs, Vector2 position){
        samples.Add((timeMs, position));
    }

    public Vector2 Sample(float renderTimeMs){
        if (samples.Count == 0){
            return Vector2.zero;
        }

        if (renderTimeMs < samples[0].timeMs){
            return samples[0].position;
        }

        if (renderTimeMs > samples[samples.Count - 1].timeMs){
            return samples[samples.Count - 1].position;
        }

        for (int i = 0; i < samples.Count - 1; i++){
            if (samples[i].timeMs <= renderTimeMs && renderTimeMs < samples[i + 1].timeMs){
                var a = samples[i];
                var b = samples[i + 1];
                float fraction = (renderTimeMs - a.timeMs) / (b.timeMs - a.timeMs);
                samples.RemoveRange(0, i);
                return Vector2.Lerp(a.position, b.position, fraction);
            }
        }
        return samples[samples.Count - 1].position;
    }
}