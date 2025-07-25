﻿namespace MLDComputing.Emulators.BBCSim._6502.Utilities;

/// <summary>
///     Design By Contract Checks.
///     Each method generates an exception or
///     a trace assertion statement if the contract is broken.
/// </summary>
/// <remarks>
///     This example shows how to call the Require method.
///     Assume DBC_CHECK_PRECONDITION is defined.
///     <code>
///  public void Test(int x)
///  {
///  	try
///  	{
/// 			Check.Require(x > 1, "x must be > 1");
/// 		}
/// 		catch (System.Exception ex)
/// 		{
/// 			Console.WriteLine(ex.ToString());
/// 		}
/// 	}
///  </code>
///     If you wish to use trace assertion statements, intended for Debug scenarios,
///     rather than exception handling then set
///     <code>Check.UseAssertions = true</code>
///     You can specify this in your application entry point and maybe make it
///     dependent on conditional compilation flags or configuration file settings, e.g.,
///     <code>
///  #if DBC_USE_ASSERTIONS
///  Check.UseAssertions = true;
///  #endif
///  </code>
///     You can direct output to a Trace listener. For example, you could insert
///     <code>
///  Trace.Listeners.Clear();
///  Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
///  </code>
///     or direct output to a file or the Event Log.
///     (Note: For ASP.NET clients use the Listeners collection
///     of the Debug, not the Trace, object and, for a Release build, only exception-handling
///     is possible.)
/// </remarks>
public static class Check
{
    #region Interface

    /// <summary>
    ///     Precondition check.
    /// </summary>
    public static void Require(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new PreconditionException(message);
        }
    }

    /// <summary>
    ///     Precondition check.
    /// </summary>
    public static void Require(bool assertion, string message, Exception inner)
    {
        if (!assertion)
        {
            throw new PreconditionException(message, inner);
        }
    }

    /// <summary>
    ///     Precondition check.
    /// </summary>
    public static void Require(bool assertion)
    {
        if (!assertion)
        {
            throw new PreconditionException("Precondition failed.");
        }
    }

    /// <summary>
    ///     Postcondition check.
    /// </summary>
    public static void Ensure(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new PostconditionException(message);
        }
    }

    /// <summary>
    ///     Postcondition check.
    /// </summary>
    public static void Ensure(bool assertion, string message, Exception inner)
    {
        if (!assertion)
        {
            throw new PostconditionException(message, inner);
        }
    }

    /// <summary>
    ///     Postcondition check.
    /// </summary>
    public static void Ensure(bool assertion)
    {
        if (!assertion)
        {
            throw new PostconditionException("Postcondition failed.");
        }
    }

    /// <summary>
    ///     Invariant check.
    /// </summary>
    public static void Invariant(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new InvariantException(message);
        }
    }

    /// <summary>
    ///     Invariant check.
    /// </summary>
    public static void Invariant(bool assertion, string message, Exception inner)
    {
        if (!assertion)
        {
            throw new InvariantException(message, inner);
        }
    }

    /// <summary>
    ///     Invariant check.
    /// </summary>
    public static void Invariant(bool assertion)
    {
        if (!assertion)
        {
            throw new InvariantException("Invariant failed.");
        }
    }

    /// <summary>
    ///     Assertion check.
    /// </summary>
    public static void Assert(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new AssertionException(message);
        }
    }

    /// <summary>
    ///     Assertion check.
    /// </summary>
    public static void Assert(bool assertion, string message, Exception inner)
    {
        if (!assertion)
        {
            throw new AssertionException(message, inner);
        }
    }

    /// <summary>
    ///     Assertion check.
    /// </summary>
    public static void Assert(bool assertion)
    {
        if (!assertion)
        {
            throw new AssertionException("Assertion failed.");
        }
    }

    #endregion // Interface
} // End Check

#region Exceptions

/// <summary>
///     Exception raised when a contract is broken.
///     Catch this exception type if you wish to differentiate between
///     any DesignByContract exception and other runtime exceptions.
/// </summary>
public class DesignByContractException : Exception
{
    protected DesignByContractException()
    {
    }

    protected DesignByContractException(string message) : base(message)
    {
    }

    protected DesignByContractException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     Exception raised when a precondition fails.
/// </summary>
public class PreconditionException : DesignByContractException
{
    /// <summary>
    ///     Precondition Exception.
    /// </summary>
    public PreconditionException()
    {
    }

    /// <summary>
    ///     Precondition Exception.
    /// </summary>
    public PreconditionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Precondition Exception.
    /// </summary>
    public PreconditionException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     Exception raised when a postcondition fails.
/// </summary>
public class PostconditionException : DesignByContractException
{
    /// <summary>
    ///     Postcondition Exception.
    /// </summary>
    public PostconditionException()
    {
    }

    /// <summary>
    ///     Postcondition Exception.
    /// </summary>
    public PostconditionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Postcondition Exception.
    /// </summary>
    public PostconditionException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     Exception raised when an invariant fails.
/// </summary>
public class InvariantException : DesignByContractException
{
    /// <summary>
    ///     Invariant Exception.
    /// </summary>
    public InvariantException()
    {
    }

    /// <summary>
    ///     Invariant Exception.
    /// </summary>
    public InvariantException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Invariant Exception.
    /// </summary>
    public InvariantException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     Exception raised when an assertion fails.
/// </summary>
public class AssertionException : DesignByContractException
{
    /// <summary>
    ///     Assertion Exception.
    /// </summary>
    public AssertionException()
    {
    }

    /// <summary>
    ///     Assertion Exception.
    /// </summary>
    public AssertionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Assertion Exception.
    /// </summary>
    public AssertionException(string message, Exception inner) : base(message, inner)
    {
    }
}

#endregion // Exception classes

// End Design By Contract