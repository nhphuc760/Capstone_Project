using System;
using Fusion;

public interface IRequirement
{   

    CheckMetResult Met();
    string GetInfor();
    void Effect();
    void Start();
    void Dispose();
    event Action Changed;
}

public struct Fraction 
{
    public Fraction(float Numerator, float Denominator)
    {
        this.Numerator = Numerator;
        this.Denominator = Denominator;
    }
    public float Numerator; //tử số
    public float Denominator; //mẫu số
    public float Ratio => Denominator != 0 ? Numerator/Denominator : default;
}



public enum CheckMetReason
{
    None,
    NotEnoughResource,
    NotEnoughDamage,
    NonUpgradable,
    NotEnoughLevel,
    NoStructure,
    Error,
    MaxLevel
}

public struct CheckMetResult
{ 
    public bool Success => Reason == CheckMetReason.None;
    public CheckMetReason Reason;
    public string Message;
}
