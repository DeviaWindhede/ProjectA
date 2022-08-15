using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Timer
{
    private float _time;
    private float _duration;

    public float Time
    {
        get { return _time; }
        set
        {
            if (value >= _time)
                _time = value;
        }
    }

    public float Duration
    {
        get { return _duration; }
    }

    public bool Expired
    {
        get { return _time >= _duration; }
    }

    public void Reset()
    {
        _time = 0;
    }

    public float Ratio
    {
        get { return _time / _duration; }
    }

    // Constructors
    public Timer(float duration, float time = 0)
    {
        _duration = duration;
        if (time < 0)
            Debug.Log("Time can not be less than 0");
        else
            _time = time;
    }

    public Timer(Timer timer)
    {
        _time = timer._time;
        _duration = timer._duration;
    }

    public override string ToString()
    {
        string s = "Time: " + _time + ", Time left: " + (Duration - _time) + ", Ratio: " + Ratio;
        return s;
    }

    public static Timer operator +(Timer t, float v)
    {
        t.Time += v;
        return t;
    }
}
