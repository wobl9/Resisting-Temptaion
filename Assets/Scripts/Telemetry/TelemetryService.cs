using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShatteredForge.Telemetry
{
    public class TelemetryService
    {
        private readonly List<string> _buffer = new();

        public void Track(string eventId, Dictionary<string, object> payload = null)
        {
            var timestamp = DateTime.UtcNow.ToString("O");
            var data = payload == null ? "{}" : MiniJson(payload);
            var line = $"{timestamp}|{eventId}|{data}";
            _buffer.Add(line);
            Debug.Log($"Telemetry: {line}");
        }

        public IReadOnlyList<string> GetBufferedEvents()
        {
            return _buffer;
        }

        private static string MiniJson(Dictionary<string, object> payload)
        {
            var parts = new List<string>();
            foreach (var entry in payload)
            {
                parts.Add($"\"{entry.Key}\":\"{entry.Value}\"");
            }

            return "{" + string.Join(",", parts) + "}";
        }
    }
}
