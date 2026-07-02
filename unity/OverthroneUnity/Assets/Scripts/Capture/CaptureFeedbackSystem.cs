using System;

namespace Overthrone
{
    public static class CaptureFeedbackSystem
    {
        public static event Action<CaptureFeedbackEvent> FeedbackEmitted;

        public static void Emit(CaptureFeedbackEvent feedbackEvent)
        {
            FeedbackEmitted?.Invoke(feedbackEvent);
        }
    }
}
