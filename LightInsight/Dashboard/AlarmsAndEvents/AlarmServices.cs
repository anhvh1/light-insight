using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;
using VideoOS.Platform.Proxy.Alarm;
using VideoOS.Platform.Proxy.AlarmClient;

namespace LightInsight.Dashboard.AlarmsAndEvents
{
    // ==========================================
    // 1. CÁC CLASS MODEL DÙNG CHUNG CHO GIAO DIỆN
    // ==========================================
    public class DailyCountData
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }

    public class SourceCountData
    {
        public string Source { get; set; }
        public int Count { get; set; }
    }

    public class TypeCountData
    {
        public string TypeName { get; set; }
        public int Count { get; set; }
    }

    // ==========================================
    // 2. LỚP DỊCH VỤ XỬ LÝ NGHIỆP VỤ MILESTONE
    // ==========================================
    public static class AlarmServices
    {
        // Lấy danh sách lịch sử báo động mới nhất
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
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetHistoricalAlarms Error: " + ex.Message);
            }

            return result;
        }

        // Đăng ký nhận báo động trực tiếp (Real-time)
        public static object RegisterForLiveAlarms(Action<VideoOS.Platform.Data.Alarm> onNewAlarmCallback)
        {
            try
            {
                return EnvironmentManager.Instance.RegisterReceiver((message, dest, source) =>
                {
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
                        catch { }
                    }
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

        // Hủy đăng ký báo động
        public static void UnregisterLiveAlarms(object receiver)
        {
            if (receiver != null)
            {
                try
                {
                    EnvironmentManager.Instance.UnRegisterReceiver(receiver);
                }
                catch { }
            }
        }

        // Đếm số lượng báo động trong ngày hôm nay
        public static int GetTodayAlarmCount()
        {
            try
            {
                var alarmClientManager = new AlarmClientManager();
                IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                if (client == null) return 0;

                DateTime startOfTodayUTC = DateTime.Today.ToUniversalTime();

                AlarmFilter filter = new AlarmFilter();
                filter.Conditions = new Condition[]
                {
                    new Condition
                    {
                        Target = VideoOS.Platform.Proxy.Alarm.Target.Timestamp,
                        Operator = VideoOS.Platform.Proxy.Alarm.Operator.GreaterThan,
                        Value = startOfTodayUTC
                    }
                };

                AlarmLine[] lines = client.GetAlarmLines(0, 10000, filter);

                if (lines != null)
                {
                    return lines.Length;
                }
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetTodayAlarmCount Error: " + ex.Message);
            }

            return 0;
        }

        // Thống kê số lượng báo động theo 7 ngày gần nhất (Cho biểu đồ Cột)
        public static List<DailyCountData> GetWeeklyAlarmCounts()
        {
            var resultList = new List<DailyCountData>();
            var stats = new Dictionary<DateTime, int>();

            DateTime today = DateTime.Today;
            for (int i = 6; i >= 0; i--)
            {
                stats.Add(today.AddDays(-i), 0);
            }

            try
            {
                var alarmClientManager = new AlarmClientManager();
                IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                if (client != null)
                {
                    DateTime startOf7DaysAgoUTC = today.AddDays(-6).ToUniversalTime();

                    AlarmFilter filter = new AlarmFilter();
                    filter.Conditions = new Condition[]
                    {
                        new Condition
                        {
                            Target = VideoOS.Platform.Proxy.Alarm.Target.Timestamp,
                            Operator = VideoOS.Platform.Proxy.Alarm.Operator.GreaterThan,
                            Value = startOf7DaysAgoUTC
                        }
                    };

                    AlarmLine[] lines = client.GetAlarmLines(0, 50000, filter);

                    if (lines != null)
                    {
                        foreach (var line in lines)
                        {
                            DateTime localDate = line.Timestamp.ToLocalTime().Date;
                            if (stats.ContainsKey(localDate))
                            {
                                stats[localDate]++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetWeeklyAlarmCounts Error: " + ex.Message);
            }

            foreach (var item in System.Linq.Enumerable.OrderBy(stats, x => x.Key))
            {
                resultList.Add(new DailyCountData
                {
                    Day = item.Key.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
                    Count = item.Value
                });
            }

            return resultList;
        }

        // Thống kê báo động theo nguồn Camera/Thiết bị (Cho biểu đồ Thanh ngang)
        public static List<SourceCountData> GetAlarmCountsBySource(int daysBack = 7)
        {
            var resultList = new List<SourceCountData>();
            var stats = new Dictionary<string, int>();

            try
            {
                var alarmClientManager = new AlarmClientManager();
                IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                if (client != null)
                {
                    DateTime startTimeUTC = DateTime.Today.AddDays(-daysBack).ToUniversalTime();

                    AlarmFilter filter = new AlarmFilter();
                    filter.Conditions = new Condition[]
                    {
                        new Condition
                        {
                            Target = VideoOS.Platform.Proxy.Alarm.Target.Timestamp,
                            Operator = VideoOS.Platform.Proxy.Alarm.Operator.GreaterThan,
                            Value = startTimeUTC
                        }
                    };

                    AlarmLine[] lines = client.GetAlarmLines(0, 50000, filter);

                    if (lines != null)
                    {
                        foreach (var line in lines)
                        {
                            string sourceName = line.SourceName;
                            if (string.IsNullOrEmpty(sourceName))
                            {
                                sourceName = "Unknown Source";
                            }

                            if (stats.ContainsKey(sourceName))
                            {
                                stats[sourceName]++;
                            }
                            else
                            {
                                stats.Add(sourceName, 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetAlarmCountsBySource Error: " + ex.Message);
            }

            // Sắp xếp số lượng từ cao xuống thấp
            foreach (var item in System.Linq.Enumerable.OrderByDescending(stats, x => x.Value))
            {
                resultList.Add(new SourceCountData
                {
                    Source = item.Key,
                    Count = item.Value
                });
            }

            return resultList;
        }

        // THỐNG KÊ THEO LOẠI BÁO ĐỘNG (ALARM TYPE)
        public static List<TypeCountData> GetAlarmCountsByType(int daysBack = 7)
        {
            var resultList = new List<TypeCountData>();
            var stats = new Dictionary<string, int>();

            try
            {
                var alarmClientManager = new AlarmClientManager();
                IAlarmClient client = alarmClientManager.GetAlarmClient(EnvironmentManager.Instance.MasterSite.ServerId);

                if (client != null)
                {
                    DateTime startTimeUTC = DateTime.Today.AddDays(-daysBack).ToUniversalTime();

                    AlarmFilter filter = new AlarmFilter();
                    filter.Conditions = new Condition[]
                    {
                        new Condition
                        {
                            Target = VideoOS.Platform.Proxy.Alarm.Target.Timestamp,
                            Operator = VideoOS.Platform.Proxy.Alarm.Operator.GreaterThan,
                            Value = startTimeUTC
                        }
                    };

                    AlarmLine[] lines = client.GetAlarmLines(0, 50000, filter);

                    if (lines != null)
                    {
                        foreach (var line in lines)
                        {
                            // Lấy tên Loại báo động (Ví dụ: Motion Detection, Line Crossing...)
                            string typeName = line.Message;

                            if (string.IsNullOrEmpty(typeName))
                            {
                                typeName = "Unknown Type";
                            }

                            if (stats.ContainsKey(typeName))
                            {
                                stats[typeName]++;
                            }
                            else
                            {
                                stats.Add(typeName, 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EnvironmentManager.Instance.Log(false, "LightInsight Widget", "GetAlarmCountsByType Error: " + ex.Message);
            }

            // Sắp xếp các loại lỗi xảy ra nhiều nhất lên đầu
            foreach (var item in System.Linq.Enumerable.OrderByDescending(stats, x => x.Value))
            {
                resultList.Add(new TypeCountData
                {
                    TypeName = item.Key,
                    Count = item.Value
                });
            }

            return resultList;
        }
    }
}