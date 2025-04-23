using UnityEngine;

public static class InterpolationMethods
{
    public static float Lerp(float start, float end, float value)
    {
        var ret = ((1.0f - value)*start) + (value*end);

        // If the start is smaller than the end
        // and the interpolated value exceeded end, clamp it to end.
        if (end < start)
        {
            if (ret < end)
            {
                ret = end;
            }
        } else
        {
            // The start is greater than the end.
            if (ret > end)
            {
                ret = end;
            }
        }

        return ret;
    }

    public static int Lerp(int start, int end, float value)
    {
        return Mathf.Clamp((int) (((1.0f - value)*start) + (value*end)), start, end);
    }

    public static float Clerp(float start, float end, float value)
    {
        float min = 0.0f;
        float max = 360.0f;
        float half = Mathf.Abs((max - min)/2.0f); //half the distance between min and max
        float retval = 0.0f;
        float diff = 0.0f;

        if ((end - start) < -half)
        {
            diff = ((max - start) + end)*value;
            retval = start + diff;
        }
        else if ((end - start) > half)
        {
            diff = -((max - end) + start)*value;
            retval = start + diff;
        }
        else retval = start + (end - start)*value;

        // Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
        return Mathf.Clamp(retval, start, end);
    }

    public static float Hermite(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value*value*(3.0f - 2.0f*value));
    }

    public static float Berp(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = (Mathf.Sin(value*Mathf.PI*(0.2f + 2.5f*value*value*value))*Mathf.Pow(1f - value, 2.2f) + value)*
                (1f + (1.2f*(1f - value)));
        return start + (end - start)*value;
    }
}
