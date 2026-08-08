using YouTubeBroadcastCreator.Core.Types.Broadcast;

namespace YouTubeBroadcastCreator.Core.API.Extensions;

public static class StreamLatencyExtensions
{
    extension(BroadcastLatency latency)
    {
        public static BroadcastLatency FromApiLowLatencySetting(bool? useLowLatency) => useLowLatency switch 
        { 
            false => BroadcastLatency.Normal, 
            true  => BroadcastLatency.Low, 
            null  => BroadcastLatency.UltraLow, 
        };

        public static BroadcastLatency FromApiLatencyPreferenceString(string? latencyPreference) => latencyPreference switch 
        { 
            "normal"    => BroadcastLatency.Normal, 
            "low"       => BroadcastLatency.Low, 
            "ultraLow"  => BroadcastLatency.UltraLow, 
            _           => BroadcastLatency.Normal
        };
        
        public bool? ToApiLowLatencySetting() => latency switch 
        { 
            BroadcastLatency.Normal   => false, 
            BroadcastLatency.Low      => true, 
            BroadcastLatency.UltraLow => null, 
            _                      => false
        };
    
        public string ToApiLatencyPreferenceString() => latency switch 
        { 
            BroadcastLatency.Normal   => "normal", 
            BroadcastLatency.Low      => "low", 
            BroadcastLatency.UltraLow => "ultraLow", 
            _                      => throw new InvalidOperationException("Invalid latency value") 
        };
    }
}