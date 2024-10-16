﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Lab.Swashbuckle.AspNetCore6;

public class QueryEmployeeRequest
{
    /// <summary>
    ///     姓名
    /// </summary>
    /// <example>小章</example>
    [Required]
    public string Name { get; set; }

    /// <summary>
    ///     年齡
    /// </summary>
    /// <example>18</example>
    public int Age { get; set; }

    /// <summary>
    ///     狀態
    /// </summary>
    /// <example>1</example>
    public State State { get; set; }
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))] // This custom converter was placed in a system namespace.
public enum State
{
    [EnumMember(Value = "UNKNOWN_DEFINITION_000")]

    None = 0,

    /// <summary>
    ///     Approved
    /// </summary>
    /// <remarks>Approved</remarks>
    // [Description("Approved")]
    [EnumMember(Value = "Approved")]
    Approved = 1,

    /// <summary>
    ///     Rejected
    /// </summary>
    [EnumMember(Value = "Rejected")]
    Rejected = 2
}