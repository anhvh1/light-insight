using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;
using VideoOS.Platform.Proxy.Alarm;
using VideoOS.Platform.Proxy.AlarmClient;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    public static class AlarmServices
    {
        public static List<VideoOS.Platform.Data.Alarm> GetHistoricalAlarms(int maxCount = 50)
        {
            var result = new List<VideoOS.Platform.Data.Alarm>();
            try
            {
                var alarmClientManager = new AlarmClientManager();
                IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                if (client == null) return result;

                AlarmFilter filter = new AlarmFilter();
                filter.Conditions = new Condition[0]; // Lấy tất cả

                AlarmLine[] lines = client.GetAlarmLines(0, maxCount, filter);

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        VideoOS.Platform.Data.Alarm fullAlarm = client.Get(line.Id);
                        if (fullAlarm != null)
                        {
                            result.Add(fullAlarm);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log vào hệ thống của Milestone thay vì Console
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetHistoricalAlarms Error: " + ex.Message);
            }

            return result;
        }

        public static object RegisterForLiveAlarms(Action<VideoOS.Platform.Data.Alarm> onNewAlarmCallback)
        {
            try
            {
                return EnvironmentManager.Instance.RegisterReceiver((message, dest, source) =>
                {
                    // 1. Nếu Milestone trả về AlarmLine (Bản rút gọn)
                    if (message.Data is VideoOS.Platform.Proxy.Alarm.AlarmLine alarmLine)
                    {
                        try
                        {
                            var alarmClientManager = new AlarmClientManager();
                            IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                            if (client != null)
                            {
                                VideoOS.Platform.Data.Alarm fullAlarm = client.Get(alarmLine.Id);
                                if (fullAlarm != null)
                                {
                                    onNewAlarmCallback?.Invoke(fullAlarm);
                                }
                            }
                        }
                        catch
                        {
                            // Bỏ qua nếu lỗi mạng gián đoạn không lấy được chi tiết
                        }
                    }
                    // 2. Dự phòng: Nếu phiên bản Milestone trả thẳng về Alarm
                    else if (message.Data is VideoOS.Platform.Data.Alarm fullAlarm)
                    {
                        onNewAlarmCallback?.Invoke(fullAlarm);
                    }

                    return null;
                },
                new MessageIdFilter(MessageId.Server.NewAlarmIndication));
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "RegisterLiveAlarms Error: " + ex.Message);
                return null;
            }
        }

        public static void UnregisterLiveAlarms(object receiver)
        {
            if (receiver != null)
            {
                try
                {
                    EnvironmentManager.Instance.UnRegisterReceiver(receiver);
                }
                catch { } // Bỏ qua lỗi ngầm khi đóng widget
            }
        }
    }
}