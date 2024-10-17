using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

// author: alex@rozgo.com
// license: have fun

// mystical Unity class
public class YieldInstruction
{
    internal IEnumerator routine = null;
    internal IAsyncEnumerator<object> asyncRoutine = null;

    internal YieldInstruction()
    {
    }

    internal bool MoveNext()
    {
        var yieldInstruction = (asyncRoutine != null ? asyncRoutine.Current : routine.Current) as YieldInstruction;

        if (yieldInstruction != null)
        {
            if (yieldInstruction.MoveNext())
            {
                return true;
            }
            else if (routine.MoveNext())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (routine.MoveNext())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    internal async Task<bool> MoveNextAsync()
    {
        var yieldInstruction = (asyncRoutine != null ? asyncRoutine.Current : routine.Current) as YieldInstruction;

        if (yieldInstruction != null)
        {
            if (await yieldInstruction.MoveNextAsync())
            {
                return true;
            }
            else if (await asyncRoutine.MoveNextAsync())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (await asyncRoutine.MoveNextAsync())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

// only used as a wrapper
public class Coroutine : YieldInstruction
{
    internal Coroutine(IEnumerator routine)
    {
        this.routine = routine;
    }
}

public class AsyncCoroutine : YieldInstruction
{
    internal AsyncCoroutine(IAsyncEnumerator<object> asyncRoutine)
    {
        this.asyncRoutine = asyncRoutine;
    }
}

// use this as a template for functions like WaitForSeconds()
public class WaitForCount : YieldInstruction
{

    int count = 0;

    public WaitForCount(int count)
    {
        this.count = count;
        this.routine = Count();
    }

    IEnumerator Count()
    {
        while (--count >= 0)
        {
            System.Console.WriteLine(count);
            yield return true;
        }
    }
}

public class WaitForSeconds : YieldInstruction
{

    float seconds = 0;

    public WaitForSeconds(float seconds)
    {
        this.seconds = seconds;
        this.routine = Tick();
    }

    IEnumerator Tick()
    {
        while (--seconds >= 0)
        {
            OS.DelayMsec(1000);
            yield return true;
        }
    }
}


public class WaitUntil : YieldInstruction
{

    Func<bool> action;

    public WaitUntil(Func<bool> action)
    {
        this.action = action;
        this.routine = Tick();
    }

    IEnumerator Tick()
    {
        while (!action())
        {
            yield return true;
        }
    }
}

// use this as the base class for enabled coroutines
public class Yielder
{

    internal List<YieldInstruction> coroutines = new List<YieldInstruction>();

    // just like Unity's MonoBehaviour.StartCoroutine
    public Coroutine StartCoroutine(IEnumerator routine)
    {
        var coroutine = new Coroutine(routine);
        coroutine.routine.MoveNext();
        coroutines.Add(coroutine);
        return coroutine;
    }

    // just like Unity's MonoBehaviour.StartCoroutine
    public async Task<AsyncCoroutine> StartAsyncCoroutine(IAsyncEnumerator<object> routine)
    {
        var coroutine = new AsyncCoroutine(routine);
        await coroutine.asyncRoutine.MoveNextAsync();
        coroutines.Add(coroutine);
        return coroutine;
    }

    // call this every frame
    public async void ProcessCoroutines()
    {
        for (int i = 0; i < coroutines.Count;)
        {
            var coroutine = coroutines[i];
            if (coroutine is Coroutine && coroutine.MoveNext())
            {
                ++i;
            }
            else if (coroutine is AsyncCoroutine && await coroutine.MoveNextAsync())
            {
                ++i;
            }
            else if (coroutines.Count > 1)
            {
                coroutines[i] = coroutines[coroutines.Count - 1];
                coroutines.RemoveAt(coroutines.Count - 1);
            }
            else
            {
                coroutines.Clear();
                break;
            }
        }
    }
}

