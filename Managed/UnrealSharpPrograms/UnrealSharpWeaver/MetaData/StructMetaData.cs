﻿using Mono.Cecil;

namespace UnrealSharpWeaver.MetaData;

public class StructMetaData : BaseMetaData
{
    public List<PropertyMetaData> Fields { get; set; }
    public StructFlags StructFlags { get; set; }
    
    // Non-serialized for JSON
    public readonly bool IsBlittableStruct;
    // End non-serialized
    
    public StructMetaData(TypeDefinition structDefinition) : base(structDefinition, WeaverHelper.UStructAttribute)
    {
        if (structDefinition.Properties.Count > 0)
        {
            throw new InvalidUnrealStructException(structDefinition, "UProperties in a UStruct must be fields, not property accessors.");
        }
        
        Fields = new List<PropertyMetaData>();
        foreach (var field in structDefinition.Fields)
        {
            if (field.IsStatic || !WeaverHelper.IsUProperty(field))
            {
                continue;
            }
            
            Fields.Add(new PropertyMetaData(field));
        }
        
        IsBlittableStruct = true;
        bool isPlainOldData = true;
        
        foreach (var prop in Fields)
        {
            if (!prop.PropertyDataType.IsBlittable)
            {
                IsBlittableStruct = false;
            }
            
            if (!prop.PropertyDataType.IsPlainOldData)
            {
                isPlainOldData = false;
            }
        }
        
        StructFlags = (StructFlags) GetFlags(structDefinition, "StructFlagsMapAttribute");

        if (isPlainOldData)
        {
            StructFlags |= StructFlags.IsPlainOldData;
            StructFlags |= StructFlags.NoDestructor;
            StructFlags |= StructFlags.ZeroConstructor;
        }

        if (IsBlittableStruct)
        {
            CustomAttribute? structAttribute = WeaverHelper.GetUStruct(structDefinition);

            if (structAttribute == null)
            {
                return;
            }

            structAttribute.Fields.Add(new CustomAttributeNamedArgument("IsBlittable", new CustomAttributeArgument(structDefinition.Module.TypeSystem.Boolean, true)));
        }
    }
}