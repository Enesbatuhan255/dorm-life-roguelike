using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed partial class EventScheduler
    {
        public EventSchedulerSnapshot CaptureRuntimeSnapshot()
        {
            var snapshot = new EventSchedulerSnapshot
            {
                minorQueuedDay = minorQueuedDay,
                majorQueuedDay = majorQueuedDay
            };

            foreach (var pair in eventCooldownUntilHour)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                snapshot.cooldownEntries.Add(new EventCooldownEntrySnapshot
                {
                    eventKey = pair.Key,
                    cooldownUntilHour = pair.Value
                });
            }

            for (var i = 0; i < scheduledFollowUps.Count; i++)
            {
                var followUp = scheduledFollowUps[i];
                if (followUp == null || string.IsNullOrWhiteSpace(followUp.FollowUpId))
                {
                    continue;
                }

                snapshot.scheduledFollowUps.Add(new ScheduledFollowUpSnapshot
                {
                    followUpId = followUp.FollowUpId,
                    triggerDay = followUp.TriggerDay
                });
            }

            foreach (var pair in pendingFollowUpRepeatCounts)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                {
                    continue;
                }

                snapshot.pendingFollowUpRepeats.Add(new FollowUpRepeatSnapshot
                {
                    followUpId = pair.Key,
                    repeatCount = pair.Value
                });
            }

            return snapshot;
        }

        public void RestoreRuntimeSnapshot(EventSchedulerSnapshot snapshot)
        {
            eventCooldownUntilHour.Clear();
            scheduledFollowUps.Clear();
            pendingFollowUpRepeatCounts.Clear();
            minorQueuedDay = -1;
            majorQueuedDay = -1;

            if (snapshot == null)
            {
                return;
            }

            minorQueuedDay = snapshot.minorQueuedDay;
            majorQueuedDay = snapshot.majorQueuedDay;

            var cooldownEntries = snapshot.cooldownEntries;
            if (cooldownEntries != null)
            {
                for (var i = 0; i < cooldownEntries.Count; i++)
                {
                    var entry = cooldownEntries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.eventKey))
                    {
                        continue;
                    }

                    eventCooldownUntilHour[entry.eventKey.Trim()] = entry.cooldownUntilHour;
                }
            }

            var followUps = snapshot.scheduledFollowUps;
            if (followUps != null)
            {
                for (var i = 0; i < followUps.Count; i++)
                {
                    var entry = followUps[i];
                    var normalizedId = NormalizeId(entry != null ? entry.followUpId : string.Empty);
                    if (string.IsNullOrWhiteSpace(normalizedId))
                    {
                        continue;
                    }

                    scheduledFollowUps.Add(new ScheduledFollowUp
                    {
                        FollowUpId = normalizedId,
                        TriggerDay = Math.Max(1, entry.triggerDay)
                    });
                }
            }

            var repeats = snapshot.pendingFollowUpRepeats;
            if (repeats != null)
            {
                for (var i = 0; i < repeats.Count; i++)
                {
                    var entry = repeats[i];
                    var normalizedId = NormalizeId(entry != null ? entry.followUpId : string.Empty);
                    if (string.IsNullOrWhiteSpace(normalizedId) || entry.repeatCount <= 0)
                    {
                        continue;
                    }

                    pendingFollowUpRepeatCounts[normalizedId] = entry.repeatCount;
                }
            }
        }

        public IReadOnlyDictionary<string, EventData> ExportEventLookup()
        {
            return eventsById;
        }

        private void HandleChoiceApplied(EventData completedEvent, EventChoice appliedChoice)
        {
            if (completedEvent == null)
            {
                return;
            }

            var choiceFollowUps = appliedChoice?.FollowUpEventIds;
            if (choiceFollowUps != null && choiceFollowUps.Count > 0)
            {
                EnqueueOrScheduleFollowUps(completedEvent, choiceFollowUps, appliedChoice.FollowUpDelayDays);
                return;
            }

            var eventFollowUps = completedEvent.FollowUpEventIds;
            if (eventFollowUps != null && eventFollowUps.Count > 0)
            {
                EnqueueOrScheduleFollowUps(completedEvent, eventFollowUps, completedEvent.FollowUpDelayDays);
            }
        }

        private void EnqueueOrScheduleFollowUps(EventData completedEvent, IReadOnlyList<string> followUpIds, int delayDays)
        {
            var normalizedDelayDays = Math.Max(0, delayDays);
            if (normalizedDelayDays == 0)
            {
                EnqueueFollowUps(completedEvent, followUpIds);
                return;
            }

            ScheduleFollowUps(completedEvent, followUpIds, normalizedDelayDays);
        }

        private void ScheduleFollowUps(EventData completedEvent, IReadOnlyList<string> followUpIds, int delayDays)
        {
            if (completedEvent == null || followUpIds == null || followUpIds.Count == 0)
            {
                return;
            }

            var triggerDay = Math.Max(1, timeManager.Day + Math.Max(0, delayDays));
            var completedEventId = NormalizeId(completedEvent.EventId);
            var scheduledCount = 0;
            for (var i = 0; i < followUpIds.Count; i++)
            {
                var followUpId = NormalizeId(followUpIds[i]);
                if (string.IsNullOrWhiteSpace(followUpId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(completedEventId) && followUpId == completedEventId)
                {
                    continue;
                }

                scheduledFollowUps.Add(new ScheduledFollowUp
                {
                    FollowUpId = followUpId,
                    TriggerDay = triggerDay
                });
                scheduledCount++;
            }

            if (scheduledCount > 0)
            {
                eventManager.PublishSystemMessage(
                    $"Bu karar hemen etkisini gostermeyebilir. {delayDays} gun icinde yeni bir gelisme olabilir.");
            }
        }

        private void EnqueueDueFollowUps(int day)
        {
            if (scheduledFollowUps.Count == 0)
            {
                return;
            }

            for (var i = scheduledFollowUps.Count - 1; i >= 0; i--)
            {
                var scheduled = scheduledFollowUps[i];
                if (scheduled == null || day < scheduled.TriggerDay)
                {
                    continue;
                }

                scheduledFollowUps.RemoveAt(i);
                TryEnqueueFollowUpById(scheduled.FollowUpId);
            }
        }

        private void EnqueueFollowUps(EventData completedEvent, IReadOnlyList<string> followUpIds)
        {
            if (completedEvent == null || followUpIds == null || followUpIds.Count == 0)
            {
                return;
            }

            var completedEventId = NormalizeId(completedEvent.EventId);
            for (var i = 0; i < followUpIds.Count; i++)
            {
                var followUpId = NormalizeId(followUpIds[i]);
                if (string.IsNullOrWhiteSpace(followUpId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(completedEventId) && followUpId == completedEventId)
                {
                    continue;
                }

                TryEnqueueFollowUpById(followUpId);
            }
        }

        private void TryEnqueueFollowUpById(string followUpId)
        {
            if (string.IsNullOrWhiteSpace(followUpId))
            {
                return;
            }

            if (!eventsById.TryGetValue(followUpId, out var followUpEvent) || followUpEvent == null)
            {
                if (missingFollowUpWarnings.Add(followUpId))
                {
                    Debug.LogWarning($"[EventScheduler] Follow-up eventId '{followUpId}' was not found in scheduler pool.");
                }

                return;
            }

            if (TryEnqueueWithCooldown(followUpEvent))
            {
                return;
            }

            pendingFollowUpRepeatCounts.TryGetValue(followUpId, out var currentCount);
            pendingFollowUpRepeatCounts[followUpId] = currentCount + 1;
            eventManager.PublishSystemMessage($"Takip olayi '{followUpEvent.Title}' yogunluk nedeniyle siraya alindi.");
        }

        private void DrainBufferedFollowUps()
        {
            if (pendingFollowUpRepeatCounts.Count == 0)
            {
                return;
            }

            var keys = new List<string>(pendingFollowUpRepeatCounts.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var followUpId = keys[i];
                if (!pendingFollowUpRepeatCounts.TryGetValue(followUpId, out var repeatCount) || repeatCount <= 0)
                {
                    pendingFollowUpRepeatCounts.Remove(followUpId);
                    continue;
                }

                if (!eventsById.TryGetValue(followUpId, out var followUpEvent) || followUpEvent == null)
                {
                    if (missingFollowUpWarnings.Add(followUpId))
                    {
                        Debug.LogWarning($"[EventScheduler] Follow-up eventId '{followUpId}' was not found in scheduler pool.");
                    }

                    pendingFollowUpRepeatCounts.Remove(followUpId);
                    continue;
                }

                while (repeatCount > 0)
                {
                    if (!TryEnqueueWithCooldown(followUpEvent))
                    {
                        break;
                    }

                    repeatCount--;
                }

                if (repeatCount > 0)
                {
                    pendingFollowUpRepeatCounts[followUpId] = repeatCount;
                }
                else
                {
                    pendingFollowUpRepeatCounts.Remove(followUpId);
                }
            }
        }
    }
}
