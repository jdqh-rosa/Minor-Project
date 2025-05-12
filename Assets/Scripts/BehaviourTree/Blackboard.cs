using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public readonly struct BlackboardKey : IEquatable<BlackboardKey>
{
    private readonly string name;
    private readonly int hashedKey;

    public BlackboardKey(string pName) {
        this.name = pName;
        hashedKey = ComputeFNV1aHash(name);
    }
    
    public bool Equals(BlackboardKey other) => hashedKey == other.hashedKey;
    
    public override bool Equals(object obj) => obj is BlackboardKey other && Equals(other);

    public override int GetHashCode() => hashedKey;
    
    public override string ToString() => name;
    
    public static bool operator ==(BlackboardKey left, BlackboardKey right) => left.hashedKey == right.hashedKey;
    public static bool operator !=(BlackboardKey left, BlackboardKey right) => !(left == right);
    
    static public int ComputeFNV1aHash(string str) {
        uint hash = 2166136261;
        foreach (char c in str) {
            hash = (hash ^ c ) * 16777619;
        }
        return unchecked((int)hash);
    }
}

[Serializable]
public class BlackboardEntry<T>
{
    public BlackboardKey Key { get; }
    public T Value { get; }
    public Type ValueType { get; }

    public BlackboardEntry(BlackboardKey pKey, T pValue) {
        Key = pKey;
        Value = pValue;
        ValueType = typeof(T);
    }
    
    public override bool Equals(object obj) => obj is BlackboardEntry<T> other && other.Key == Key;
    public override int GetHashCode() => Key.GetHashCode();
    
}

[Serializable]
public class Blackboard
{
    Dictionary<string, BlackboardKey> keys = new();
    private Dictionary<BlackboardKey, object> entries = new();


    public void Debug() {
        foreach (var entry in entries) {
            var entryType = entry.Value.GetType();

            if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(BlackboardEntry<>)) {
                var _valueProperty = entryType.GetProperty("Value");
                if (_valueProperty == null) continue;
                var _value = _valueProperty.GetValue(entry.Value);
                UnityEngine.Debug.Log($"Key: {entry.Key} Value: {_value}  ");
            }
        }
    }
    
    public bool TryGetValue<T>(CommonKeys pKeyNum, out T pValue)
    {
        return TryGetValue(GetOrRegisterKey(pKeyNum), out pValue);
    }
    public bool TryGetValue<T>(BlackboardKey pKey, out T pValue)
    {
        if (entries.TryGetValue(pKey, out var entry) && entry is BlackboardEntry<T> castedEntry) {
            pValue = castedEntry.Value;
            return true;
        }
        
        pValue = default;
        return false;
    }

    public void SetValue<T>(BlackboardKey pKey, T pValue) {
        entries[pKey] = new BlackboardEntry<T>(pKey, pValue);
    }

    public BlackboardKey GetOrRegisterKey(CommonKeys pKeyNum) {
        return GetOrRegisterKey(pKeyNum.ToString());
    }
    
    public BlackboardKey GetOrRegisterKey(string pKeyName) {
        //todo: check for null, empty pKeyName or empty value

        if (!keys.TryGetValue(pKeyName, out BlackboardKey pKey)) {
            pKey = new BlackboardKey(pKeyName);
            keys[pKeyName] = pKey;
        }
        
        return pKey;
    }

    public void SetKeyValue<T>(CommonKeys pKeyNum, T pValue)
    {
        BlackboardKey _key = GetOrRegisterKey(pKeyNum);
        SetValue(_key, pValue);
    }
    
    public bool ContainsKey(BlackboardKey key) => entries.ContainsKey(key);
    
    public void Remove(BlackboardKey key) => entries.Remove(key);
}

[CreateAssetMenu(fileName = "New Blackboard Data", menuName = "BlackBoard/Blackboard Data")]
public class BlackboardData : ScriptableObject
{
    public List<BlackboardEntryData> Entries = new();

    public void SetValuesOnBlackboard(Blackboard blackboard) {
        foreach (var entry in Entries) {
            entry.SetValueOnBlackboard(blackboard);
        }
    }
}

[Serializable]
public class BlackboardEntryData : ISerializationCallbackReceiver
{
    public string KeyName;
    public AnyValue.ValueType ValueType;
    public AnyValue Value;

    public void SetValueOnBlackboard(Blackboard blackboard) {
        var _key = blackboard.GetOrRegisterKey(KeyName);
        setValueDispatchTable[Value.Type](blackboard, _key, Value);
    }

    private static Dictionary<AnyValue.ValueType, Action<Blackboard, BlackboardKey, AnyValue>> setValueDispatchTable =
        new()
        {
            { AnyValue.ValueType.Int, (blackboard, key, anyValue) => blackboard.SetValue<int>(key, anyValue) },
            { AnyValue.ValueType.Float, (blackboard, key, anyValue) => blackboard.SetValue<float>(key, anyValue) },
            { AnyValue.ValueType.Bool, (blackboard, key, anyValue) => blackboard.SetValue<bool>(key, anyValue) },
            { AnyValue.ValueType.String, (blackboard, key, anyValue) => blackboard.SetValue<string>(key, anyValue) },
            { AnyValue.ValueType.Vector3, (blackboard, key, anyValue) => blackboard.SetValue<Vector3>(key, anyValue) },
        };

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() => Value.Type = ValueType;
}

[Serializable]
public struct AnyValue
{ 
    public enum ValueType {Int, Float, Bool, String, Vector3 }
    public ValueType Type;
    
    public int IntValue;
    public float FloatValue;
    public bool BoolValue;
    public string StringValue;
    public Vector3 Vector3Value;
    
    public static implicit operator int(AnyValue value) => value.ConvertValue<int>();
    public static implicit operator float(AnyValue value) => value.ConvertValue<float>();
    public static implicit operator bool(AnyValue value) => value.ConvertValue<bool>();
    public static implicit operator string(AnyValue value) => value.ConvertValue<string>();
    public static implicit operator Vector3(AnyValue value) => value.ConvertValue<Vector3>();

    T ConvertValue<T>() {
        return Type switch
        {
            ValueType.Int => AsInt<T>(IntValue),
            ValueType.Float => AsFloat<T>(FloatValue),
            ValueType.Bool => AsBool<T>(BoolValue),
            ValueType.String => (T)(object)(StringValue),
            ValueType.Vector3 => (T)(object)(Vector3Value),
            _ => throw new NotSupportedException($"Type {typeof(T)} is not supported.")
        };
    }
    
    T AsInt <T>(int value) => typeof(T) == typeof(int) && value is T correctType ? correctType : default;
    T AsFloat <T>(float value) => typeof(T) == typeof(float) && value is T correctType ? correctType : default;
    T AsBool <T>(bool value) => typeof(T) == typeof(bool) && value is T correctType ? correctType : default;

}


public enum CommonKeys
{
    Error,
    AgentSelf,
    AttackActions,
    ChosenAction,
    ChosenAttack,
    ChosenFaceAngle,
    ChosenPosition,
    ChosenTarget,
    ChosenWeaponAngle,
    DetectedAttack,
    FindRadius,
    KnownAllies,
    KnownEnemies,
    KnownTargets,
    LastAllyPosition,
    PatrolCoolDown,
    PatrolPoints,
    SelfHealth,
    TargetAlly,
    TargetEnemy,
    TargetObject,
    TargetPosition,
    VisibleAllies,
    VisibleEnemies,
    VisibleTargets,
    
}