using System;
using System.Collections.Generic;

namespace Telegram.Bot.States;

public class ChatState
{
    private static readonly Dictionary<string, string?> emptyLabels = [];

    private Dictionary<string, string?> labels;

    public long ChatId { get; }
    public string StateName { get; }

    public IReadOnlyDictionary<string, string?> Labels => labels;
    public bool IsDefault => string.Equals(StateName, Constants.DefaultStateName, StringComparison.OrdinalIgnoreCase);
    public bool IsChanged { get; private set; }

    public ChatState(long chatId, string stateName, bool isChanged = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        ChatId = chatId;
        StateName = stateName;
        IsChanged = isChanged;
        labels = emptyLabels;
    }

    public ChatState AddOrUpdateLabel(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (labels == null || ReferenceEquals(labels, emptyLabels))
            labels = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        labels[key] = value;

        return this;
    }

    public ChatState RemoveLabel(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (labels != null && labels.ContainsKey(key))
            labels.Remove(key);

        return this;
    }

    /// <summary>
    /// Update state's labels. If called with <paramref name="labels"/> = null then all labels will be removed.
    /// </summary>
    public ChatState WithLabels(IEnumerable<KeyValuePair<string, string?>>? labels)
    {
        if (labels == null)
        {
            this.labels = emptyLabels;
            return this;
        }

        foreach(var kv in labels)
            AddOrUpdateLabel(kv.Key, kv.Value);

        return this;
    }

    public ChatState Same() => new ChatState(ChatId, StateName, isChanged: false).WithLabels(Labels);

    public ChatState New(string stateName) => new(ChatId, stateName, isChanged: true);

    public static ChatState Default(long chatId) => new(chatId, Constants.DefaultStateName);
}
