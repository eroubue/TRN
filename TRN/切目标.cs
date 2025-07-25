using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Triggernometry;
using System.Linq;
using Triggernometry.PluginBridges;
// 注册具名回调
RealPlugin.plug.UnregisterNamedCallback("Nag0mi_LogsPolice_any");
RealPlugin.plug.RegisterNamedCallback(
    "Nag0mi_LogsPolice_any",
    new Func<object, string, Task>(async (_, param) =>
    {
        try
        {
            var arr = param.Split(' ');
            if (arr.Length < 5)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e 参数不足，格式为 角色名 服务器 副本ID 职业名 metric");
                return;
            }
            string name = arr[0];
            string server = arr[1];
            string encounterIdStr = arr[2];
            int encounterId = 0;
            // 支持副本名转ID
            if (!int.TryParse(encounterIdStr, out encounterId) || encounterId == 0)
            {
                if (FFLogsV2ApiHelper.EncounterNameToId != null && FFLogsV2ApiHelper.EncounterNameToId.TryGetValue(encounterIdStr, out var mappedId))
                {
                    encounterId = mappedId;
                }
                else
                {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_LogsPolice_any] encounterId必须为数字或常用副本名");
                    return;
                }
            }
            string specName = arr[3];
            string metric = arr[4].ToLower(); // 保证小写
            string result = await FFLogsV2ApiHelper.QueryDpsAsync(name, server, encounterId, specName, metric);
            RealPlugin.plug.InvokeNamedCallback("command", $"/e {result}");
        }
        catch (Exception ex)
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e 查询异常: {ex.Message}");
        }
    }),
    null,
    registrant: "FFLogs V2 API"
);



// 合并个人与小队查询为Nag0mi_LogsPolice具名回调（参数可选，支持自动识别）
RealPlugin.plug.UnregisterNamedCallback("Nag0mi_LogsPolice");
RealPlugin.plug.RegisterNamedCallback(
    "Nag0mi_LogsPolice",
    new Func<object, string, Task>(async (_, param) =>
    {
        try
        {
            var arr = (param ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length < 1)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e 参数不足，格式为 mode [metric] [name] [server] [specName] [encounterId]");
                return;
            }
            string mode = arr[0].Trim().ToLower();
            string metric = arr.Length > 1 && !string.IsNullOrWhiteSpace(arr[1]) ? arr[1].Trim().ToLower() : "rdps";
            if (mode == "self")
            {
                string name = "", server = "", specName = "", encounterIdStr = "";
                int encounterId = 0;

                // self metric encounterId specName
                if (arr.Length >= 3)
                {
                    encounterIdStr = arr[2];
                    specName = arr.Length >= 4 ? arr[3] : "";
                    // 新增：支持副本名转ID
                    if (!int.TryParse(encounterIdStr, out encounterId) || encounterId == 0)
                    {
                        // 尝试副本名转ID
                        if (FFLogsV2ApiHelper.EncounterNameToId != null && FFLogsV2ApiHelper.EncounterNameToId.TryGetValue(encounterIdStr, out var mappedId))
                        {
                            encounterId = mappedId;
                        }
                        else
                        {
                            RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_LogsPolice] encounterId不能为空且必须为数字或常用副本名");
                            return;
                        }
                    }
                }
                else
                {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_LogsPolice] 参数不足，格式为 self metric encounterId [specName]");
                    return;
                }

                // 自动识别name和server
                try
                {
                    var me = Triggernometry.FFXIV.Entity.GetMyself();
                    name = me?.Name ?? "";
                    int? worldId = me?.WorldID;
                    if (worldId != null && FFLogsV2ApiHelper.WorldIdToName.TryGetValue(worldId.Value, out var cnName))
                        server = cnName;
                }
                catch { }
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(server))
                {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_LogsPolice] 未能自动识别角色名或服务器");
                    return;
                }

                // specName可为空，自动识别
                if (string.IsNullOrWhiteSpace(specName))
                {
                    try
                    {
                        var me = Triggernometry.FFXIV.Entity.GetMyself();
                        specName = me?.Job.NameEN ?? "";
                    }
                    catch { }
                }

                try
                {
                    string result = await FFLogsV2ApiHelper.QueryDpsAsync(name, server, encounterId, specName, metric);
                    RealPlugin.plug.InvokeNamedCallback("command", $"/e {result}");
                }
                catch (Exception ex)
                {
                    RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_LogsPolice] {name} 查询异常: {ex.Message}");
                }
            }
            else if (mode == "party")
            {
                int encounterId = 0;
                if (arr.Length == 3 && int.TryParse(arr[2], out encounterId))
                {
                    // party metric encounterId
                }
                else if (arr.Length == 2)
                {
                    // party metric
                    // encounterId为0，直接返回空
                    return;
                }
                else if (arr.Length > 3)
                {
                    string encounterIdStr = arr[2];
                    if (!string.IsNullOrWhiteSpace(encounterIdStr))
                        int.TryParse(encounterIdStr, out encounterId);
                }
                // 获取小队成员列表
                var party = Triggernometry.FFXIV.Entity.GetEntities().Where(e => e.InParty).ToList();
                if (party == null || party.Count == 0) {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_LogsPolice] 未获取到小队成员");
                    return;
                }
                foreach (var member in party)
                {
                    string name = member.Name;
                    int? worldId = member.WorldID;
                    string server = "";
                    if (worldId != null && FFLogsV2ApiHelper.WorldIdToName.TryGetValue(worldId.Value, out var cnName))
                        server = cnName;
                    else
                    {
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_LogsPolice] 队员{ name } WorldID { worldId } 未找到对应服务器名，已跳过");
                        continue;
                    }
                    try {
                        string json = await FFLogsV2ApiHelper.QueryDpsRawAsync(name, server, encounterId, "", metric); // specName始终传空
                        // 解析bestAmount
                        double best = 0, medPct = 0, pct = 0;
                        int kills = 0;
                        int bestStart = json.IndexOf("\"bestAmount\":");
                        if (bestStart != -1) {
                            bestStart += 13;
                            int bestEnd = json.IndexOf(",", bestStart);
                            if (bestEnd == -1) bestEnd = json.IndexOf("}", bestStart);
                            string bestStr = json.Substring(bestStart, bestEnd - bestStart).Trim();
                            double.TryParse(bestStr, out best);
                        }
                        int medStart = json.IndexOf("\"medianPerformance\":");
                        if (medStart != -1) {
                            medStart += 20;
                            int medEnd = json.IndexOf(",", medStart);
                            if (medEnd == -1) medEnd = json.IndexOf("}", medStart);
                            string medStr = json.Substring(medStart, medEnd - medStart).Trim();
                            double.TryParse(medStr, out medPct);
                        }
                        int killsStart = json.IndexOf("\"totalKills\":");
                        if (killsStart != -1) {
                            killsStart += 12;
                            while (killsStart < json.Length && (json[killsStart] == ':' || json[killsStart] == ' ')) killsStart++;
                            int killsEnd = json.IndexOf(",", killsStart);
                            if (killsEnd == -1) killsEnd = json.IndexOf("}", killsStart);
                            string killsStr = json.Substring(killsStart, killsEnd - killsStart).Trim();
                            int.TryParse(killsStr, out kills);
                        }
                        int pctStart = json.IndexOf("\"rankPercent\":");
                        if (pctStart != -1) {
                            pctStart += 14;
                            int pctEnd = json.IndexOf(",", pctStart);
                            if (pctEnd == -1) pctEnd = json.IndexOf("}", pctStart);
                            string pctStr = json.Substring(pctStart, pctEnd - pctStart).Trim();
                            double.TryParse(pctStr, out pct);
                        }
                        if (kills == 0)
                        {
                            RealPlugin.plug.InvokeNamedCallback("command", $"/e {name}({server}): 未过本");
                        }
                        else
                        {
                            RealPlugin.plug.InvokeNamedCallback("command", $"/e {name}({server}): 最高dps:{(int)Math.Round(best)}，med%:{(int)Math.Round(medPct)}，通关数:{kills}，best%:{(int)Math.Round(pct)}");
                        }
                    } catch (Exception ex) {
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_LogsPolice] {name} 查询异常: {ex.Message}");
                    }
                }
            }
            else
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e mode参数必须为self或party");
            }
        }
        catch (Exception ex)
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e Nag0mi_LogsPolice异常: {ex.Message}");
        }
    }),
    null,
    registrant: "FFLogs V2 API"
);
RealPlugin.plug.UnregisterNamedCallback("Nag0mi_SumemoPolice");
RealPlugin.plug.RegisterNamedCallback(
    "Nag0mi_SumemoPolice",
    new Func<object, string, Task>(async (_, param) =>
    {
        try
        {
            var arr = (param ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length < 1)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e 参数不足，格式为 [mode] zoneid");
                return;
            }
            string mode = "party";
            string zoneidInput = null;
            if (arr.Length == 1)
            {
                zoneidInput = arr[0];
            }
            else
            {
                mode = arr[0].Trim().ToLower();
                zoneidInput = arr[1];
            }
            // 支持简称/中文名/数字
            int mapId = 0;
            if (FFLogsV2ApiHelper.EncounterToMapId.TryGetValue(zoneidInput, out var foundMapId))
                mapId = foundMapId;
            else if (int.TryParse(zoneidInput, out var id))
                mapId = id;
            else
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice] 副本ID必须为数字、简称或副本名");
                return;
            }
            if (mapId == 0)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice] 未找到副本对应的地图ID");
                return;
            }

            if (mode == "self")
            {
                // 自动识别自己
                string name = "", server = "";
                try
                {
                    var me = Triggernometry.FFXIV.Entity.GetMyself();
                    name = me?.Name ?? "";
                    int? worldId = me?.WorldID;
                    if (worldId != null && FFLogsV2ApiHelper.WorldIdToName.TryGetValue(worldId.Value, out var cnName))
                        server = cnName;
                }
                catch { }
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(server))
                {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice] 未能自动识别角色名或服务器");
                    return;
                }
                try
                {
                    string result = await FFLogsV2ApiHelper.QuerySumemoProgressAsync(name, server, zoneidInput);
                    RealPlugin.plug.InvokeNamedCallback("command", $"/e {result}");
                }
                catch (Exception ex)
                {
                    RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_SumemoPolice] {name} 查询异常: {ex.Message}");
                }
            }
            else // party
            {
                // 获取小队成员
                var party = Triggernometry.FFXIV.Entity.GetEntities().Where(e => e.InParty).ToList();
                if (party == null || party.Count == 0)
                {
                    RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice] 未获取到小队成员");
                    return;
                }
                foreach (var member in party)
                {
                    string name = member.Name;
                    int? worldId = member.WorldID;
                    string server = "";
                    if (worldId != null && FFLogsV2ApiHelper.WorldIdToName.TryGetValue(worldId.Value, out var cnName))
                        server = cnName;
                    else
                    {
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_SumemoPolice] 队员{ name } WorldID { worldId } 未找到对应服务器名，已跳过");
                        continue;
                    }
                    try
                    {
                        string result = await FFLogsV2ApiHelper.QuerySumemoProgressAsync(name, server, zoneidInput);
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e {result}");
                    }
                    catch (Exception ex)
                    {
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_SumemoPolice] {name} 查询异常: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e [Nag0mi_SumemoPolice] 异常: {ex.Message}");
        }
    }),
    null,
    registrant: "SumemoPolice"
);
RealPlugin.plug.UnregisterNamedCallback("Nag0mi_SumemoPolice_any");
RealPlugin.plug.RegisterNamedCallback(
    "Nag0mi_SumemoPolice_any",
    new Func<object, string, Task>(async (_, param) =>
    {
        try
        {
            var arr = (param ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length < 3)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e 参数不足，格式为 角色名 服务器名 副本ID");
                return;
            }
            string name = arr[0];
            string server = arr[1];
            string encounterIdStr = arr[2];
            int mapId = 0;
            if (FFLogsV2ApiHelper.EncounterToMapId.TryGetValue(encounterIdStr, out var foundMapId))
            {
                mapId = foundMapId;
            }
            else if (int.TryParse(encounterIdStr, out var id))
            {
                // 允许直接输入地图ID
                mapId = id;
            }
            else
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice_any] 副本ID必须为数字、简称或副本名");
                return;
            }
            if (mapId == 0)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Nag0mi_SumemoPolice_any] 未找到副本对应的地图ID");
                return;
            }
            string result = await FFLogsV2ApiHelper.QuerySumemoProgressAsync(name, server, encounterIdStr);
            RealPlugin.plug.InvokeNamedCallback("command", $"/e {result}");
        }
        catch (Exception ex)
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e 查询异常: {ex.Message}");
        }
    }),
    null,
    registrant: "SumemoPolice"
);


public static class FFLogsV2ApiHelper
{
    public static readonly Dictionary<int, string> WorldIdToName = new Dictionary<int, string>
    {
        { 1201, "红茶川" },
        { 1200, "亚马乌罗提" },
        { 1192, "水晶塔" },
        { 1186, "伊修加德" },
        { 1183, "银泪湖" },
        { 1180, "太阳神岸" },
        { 1179, "琥珀原" },
        { 1178, "柔风海湾" },
        { 1177, "潮风亭" },
        { 1176, "梦羽宝境" },
        { 1175, "晨曦王座" },
        { 1174, "沃仙曦染" },
        { 1173, "宇宙和音" },
        { 1172, "白银乡" },
        { 1171, "神拳痕" },
        { 1170, "延夏" },
        { 1169, "静语庄园" },
        { 1168, "拉诺西亚" },
        { 1167, "红玉海" },
        { 1166, "龙巢神殿" },
        { 1121, "拂晓之间" },
        { 1116, "萌芽池" },
        { 1113, "旅人栈桥" },
        { 1084, "静语庄园" },
        { 1082, "神意之地" },
        { 1062, "白金幻象" },
        { 1061, "白金幻象" },
        { 1045, "摩杜纳" },
        { 1044, "幻影群岛" },
        { 1043, "紫水栈桥" },
        { 1042, "拉诺西亚" },
        // ...如有遗漏请补全...
    };

    // 职业英文名映射表（带空格→FFLogs格式）
    static readonly Dictionary<string, string> SpecNameMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Dark Knight", "DarkKnight" },
            { "DK", "DarkKnight" },
            { "DRK", "DarkKnight" },
            { "暗黑骑士", "DarkKnight" },
            { "黑暗骑士", "DarkKnight" },
            { "暗骑", "DarkKnight" },
            { "黑骑", "DarkKnight" },
            { "黑骑士", "DarkKnight" },
            { "暗骑士", "DarkKnight" },
            { "枪刃", "Gunbreaker" },
            { "枪", "Gunbreaker" },
            { "GNB", "Gunbreaker" },
            { "绝枪战士", "Gunbreaker" },
            { "绝枪", "Gunbreaker" },
            { "骑士", "Paladin" },
            { "PLD", "Paladin" },
            { "骑", "Paladin" },
            { "白骑", "Paladin" },
            { "白骑士", "Paladin" },
            { "战士", "Warrior" },
            { "战", "Warrior" },
            { "WAR", "Warrior" },
            { "White Mage", "WhiteMage" },
            { "WHM", "WhiteMage" },
            { "白魔", "WhiteMage" },
            { "白魔法师", "WhiteMage" },
            { "白", "WhiteMage" },
            { "学者", "Scholar" },
            { "学", "Scholar" },
            { "SCH", "Scholar" },
            { "占星术师", "Astrologian" },
            { "AST", "Astrologian" },
            { "占星", "Astrologian" },
            { "占", "Astrologian" },
            { "贤者", "Sage" },
            { "SGE", "Sage" },
            { "贤", "Sage" },
            { "Black Mage", "BlackMage" },
            { "BLM", "BlackMage" },
            { "黑魔", "BlackMage" },
            { "黑魔法师", "BlackMage" },
            { "黑", "BlackMage" },
            { "召唤", "Summoner" },
            { "SMN", "Summoner" },
            { "召唤师", "Summoner" },
            { "召", "Summoner" },
            { "Red Mage", "RedMage" },
            { "RDM", "RedMage" },
            { "红魔", "RedMage" },
            { "红魔法师", "RedMage" },
            { "红", "RedMage" },
            { "赤", "RedMage" },
            { "赤魔法师", "RedMage" },
            { "赤魔", "RedMage" },
            { "吟游诗人", "Bard" },
            { "BRD", "Bard" },
            { "诗人", "Bard" },
            { "诗", "Bard" },
            { "DNC", "Dancer" },
            { "舞者", "Dancer" },
            { "舞", "Dancer" },
            { "机工", "Machinist" },
            { "MCH", "Machinist" },
            { "机工士", "Machinist" },
            { "机", "Machinist" },
            { "龙骑士", "Dragoon" },
            { "DRG", "Dragoon" },
            { "龙", "Dragoon" },
            { "龙骑", "Dragoon" },
            { "武僧", "Monk" },
            { "武", "Monk" },
            { "MNK", "Monk" },
            { "僧", "Monk" },
            { "忍", "Ninja" },
            { "NIN", "Ninja" },
            { "忍者", "Ninja" },
            { "镰刀", "Reaper" },
            { "RPR", "Reaper" },
            { "镰", "Reaper" },
            { "钐镰客", "Reaper" },
            { "武士", "Samurai" },
            { "SAM", "Samurai" },
            { "侍", "Samurai" },
            { "盘", "Samurai" },
            { "盘子", "Samurai" },
            { "Blue Mage", "BlueMage" },
            { "青魔", "BlueMage" },
            { "BLU", "BlueMage" },
            { "青魔法师", "BlueMage" },
            { "青", "BlueMage" },
            { "Rogue", "Ninja" },
            { "Gladiator", "Paladin" },
            { "Conjurer", "WhiteMage" },
            { "幻术师", "WhiteMage" },
            { "Arcanist", "Summoner" },
            { "Marauder", "Warrior" },
            { "Lancer", "Dragoon" },
            { "Pugilist", "Monk" },
            { "Thaumaturge", "BlackMage" },
            { "Archer", "Bard" },
            { "画家", "Pictomancer" },
            { "PCT", "Pictomancer" },
            { "画", "Pictomancer" },
            { "绘灵法师", "Pictomancer" },
            { "VPR", "Viper" },
            { "蛇", "Viper" },
            { "蝰蛇", "Viper" },
            { "蝰蛇剑士", "Viper" },
            // ...如有其它职业请补全...
        };

    // 职业英文→中文全称映射
    public static readonly Dictionary<string, string> SpecNameEnToZh =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "DarkKnight", "暗黑骑士" },
            { "Gunbreaker", "绝枪战士" },
            { "Paladin", "骑士" },
            { "Warrior", "战士" },
            { "WhiteMage", "白魔法师" },
            { "Scholar", "学者" },
            { "Astrologian", "占星术师" },
            { "Sage", "贤者" },
            { "BlackMage", "黑魔法师" },
            { "Summoner", "召唤师" },
            { "RedMage", "赤魔法师" },
            { "Bard", "吟游诗人" },
            { "Dancer", "舞者" },
            { "Machinist", "机工士" },
            { "Dragoon", "龙骑士" },
            { "Monk", "武僧" },
            { "Ninja", "忍者" },
            { "Reaper", "钐镰客" },
            { "Samurai", "武士" },
            { "BlueMage", "青魔法师" },
            { "Pictomancer", "绘灵法师" },
            { "Viper", "蝰蛇剑士" }
            // ...如有其它职业请补全...
        };

    // clientId和clientSecret通过GetScalarVariable获取
    private static string GetClientId()
    {
        var v = Interpreter.StaticHelpers.GetScalarVariable(false, "FFLOGS_CLIENT_ID");
        if (string.IsNullOrEmpty(v))
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e 未设置FFLOGS_CLIENT_ID变量 ");
            throw new Exception("未设置FFLOGS_CLIENT_ID变量");
        }

        return v;
    }

    private static string GetClientSecret()
    {
        var v = Interpreter.StaticHelpers.GetScalarVariable(false, "FFLOGS_CLIENT_SECRET");
        if (string.IsNullOrEmpty(v))
        {
            RealPlugin.plug.InvokeNamedCallback("command", $"/e 未设置FFLOGS_CLIENT_SECRET变量 ");
            throw new Exception("未设置FFLOGS_CLIENT_SECRET变量");
        }


        return v;
    }

    private static string accessToken = null;
    private static DateTime tokenExpire = DateTime.MinValue;
    private static readonly HttpClient _client = new HttpClient();

    // 获取access_token（健壮性加强）
    public static async Task<string> GetAccessTokenAsync()
    {
        if (accessToken != null && DateTime.Now < tokenExpire)
            return accessToken;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.fflogs.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };
        string clientId = GetClientId();
        string clientSecret = GetClientSecret();
        string basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        try
        {
            var response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            //RealPlugin.plug.InvokeNamedCallback("command", $"/e Token状态码: {response.StatusCode}");
            try
            {
                // if (!string.IsNullOrEmpty(responseContent))
                //RealPlugin.plug.InvokeNamedCallback("command", "/e TokenResp: " + (responseContent.Length > 400 ? responseContent.Substring(0, 400) : responseContent));
                // 隐藏所有原始json、状态码等调试输出
                //else
                //RealPlugin.plug.InvokeNamedCallback("command", "/e TokenResp为空");
                // 隐藏所有原始json、状态码等调试输出
            }
            catch (Exception ex)
            {
                //RealPlugin.plug.InvokeNamedCallback("command", "/e [TokenResp输出异常] " + ex.Message);
                // 隐藏所有原始json、状态码等调试输出
            }

            response.EnsureSuccessStatusCode();
            // 解析access_token
            int tokenStart = responseContent.IndexOf("\"access_token\":\"");
            if (tokenStart == -1)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Token解析失败] 未找到access_token字段");
                throw new Exception("未找到access_token字段");
            }

            tokenStart += 16;
            int tokenEnd = responseContent.IndexOf("\"", tokenStart);
            if (tokenEnd == -1)
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Token解析失败] 未找到access_token结束引号");
                throw new Exception("未找到access_token结束引号");
            }

            accessToken = responseContent.Substring(tokenStart, tokenEnd - tokenStart);
            // 解析expires_in
            int expiresIn = 0; // 外部声明
            int idx = responseContent.IndexOf("\"expires_in\"");
            if (idx >= 0)
            {
                int start = responseContent.IndexOf(":", idx);
                if (start >= 0)
                {
                    start++; // 跳过冒号
                    // 跳过空格和引号
                    while (start < responseContent.Length &&
                           (responseContent[start] == ' ' || responseContent[start] == '\"')) start++;
                    int end = responseContent.IndexOfAny(new char[] { ',', '}', '\n', '\r' }, start);
                    if (end == -1) end = responseContent.Length;
                    string value = responseContent.Substring(start, end - start).Trim();
                    if (!int.TryParse(value, out expiresIn))
                    {
                        RealPlugin.plug.InvokeNamedCallback("command", $"/e [Token解析失败] expires_in不是数字: {value}");
                        throw new Exception("expires_in不是数字");
                    }
                }
            }
            else
            {
                RealPlugin.plug.InvokeNamedCallback("command", "/e [Token解析失败] 未找到expires_in字段");
                throw new Exception("未找到expires_in字段");
            }

            tokenExpire = DateTime.Now.AddSeconds(expiresIn - 60);
            return accessToken;
        }
        catch (Exception ex)
        {
            // RealPlugin.plug.InvokeNamedCallback("command", $"/e Token异常: {ex.Message}");
            throw;
        }
    }

    // 查询dps（region固定为CN，健壮性加强）
    public static async Task<string> QueryDpsAsync(string name, string server, int encounterId, string specName,
        string metric)
    {
        string region = "CN";
        string token = await GetAccessTokenAsync();
        // 不再自动识别encounterId，未传入则直接返回空
        if (encounterId == 0)
        {
            return "";
        }

        // specName映射转换，始终在最终传递前做
        if (!string.IsNullOrWhiteSpace(specName) && SpecNameMap.TryGetValue(specName.Trim(), out var mapped))
            specName = mapped;
        var query =
            @"query($name: String!, $server: String!, $serverRegion: String!, $encounterID: Int!, $metric: CharacterRankingMetricType!, $specName: String) { characterData { character(name: $name, serverSlug: $server, serverRegion: $serverRegion) { encounterRankings(encounterID: $encounterID, metric: $metric, specName: $specName) } } }";
        string jsonPayload =
            $"{{\"query\":\"{query.Replace("\"", "\\\"")}\",\"variables\":{{\"name\":\"{name}\",\"server\":\"{server}\",\"serverRegion\":\"{region}\",\"encounterID\":{encounterId},\"metric\":\"{metric}\",\"specName\":\"{specName}\"}}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.fflogs.com/api/v2/client")
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Accept", "application/json");
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        // 隐藏所有原始json、状态码等调试输出
        if (json.Contains("\"encounterRankings\":null"))
            return $"{name}({specName}): 未查询到数据";
        double best = 0;
        int kills = 0;
        double pct = 0;
        int bestStart = json.IndexOf("\"bestAmount\":");
        if (bestStart != -1)
        {
            bestStart += 13;
            int bestEnd = json.IndexOf(",", bestStart);
            if (bestEnd == -1) bestEnd = json.IndexOf("}", bestStart);
            string bestStr = json.Substring(bestStart, bestEnd - bestStart).Trim();
            if (!double.TryParse(bestStr, out best))
                ; // 不再输出调试信息
        }

        int killsStart = json.IndexOf("\"totalKills\":");
        if (killsStart != -1)
        {
            killsStart += 12;
            // 跳过冒号和空格
            while (killsStart < json.Length && (json[killsStart] == ':' || json[killsStart] == ' ')) killsStart++;
            int killsEnd = json.IndexOf(",", killsStart);
            if (killsEnd == -1) killsEnd = json.IndexOf("}", killsStart);
            string killsStr = json.Substring(killsStart, killsEnd - killsStart).Trim();
            if (!int.TryParse(killsStr, out kills))
                ; // 不再输出调试信息
        }

        int pctStart = json.IndexOf("\"rankPercent\":");
        if (pctStart != -1)
        {
            pctStart += 14;
            int pctEnd = json.IndexOf(",", pctStart);
            if (pctEnd == -1) pctEnd = json.IndexOf("}", pctStart);
            string pctStr = json.Substring(pctStart, pctEnd - pctStart).Trim();
            if (!double.TryParse(pctStr, out pct))
                ; // 不再输出调试信息
        }

        // 染色逻辑
        string color = "灰";
        double rtn = pct;
        if (rtn >= 99.9)
            color = "金";
        else if (rtn >= 99 && rtn < 99.9)
            color = "粉";
        else if (rtn >= 95 && rtn < 99)
            color = "橙";
        else if (rtn >= 75 && rtn < 95)
            color = "紫";
        else if (rtn >= 50 && rtn < 75)
            color = "蓝";
        else if (rtn >= 25 && rtn < 50)
            color = "绿";
        // 中文职业名
        string specNameZh = specName;
        if (!string.IsNullOrWhiteSpace(specName) && SpecNameEnToZh.TryGetValue(specName, out var zh1))
            specNameZh = zh1;
        // encounterId中文全称
        string encounterNameZh = encounterId.ToString();
        foreach (var kv in EncounterNameToId)
        {
            if (kv.Value == encounterId && kv.Key.Any(c => c > 127) && !kv.Key.Any(c => c < 128)) // 只取第一个全中文名
            {
                encounterNameZh = kv.Key;
                break;
            }
        }

        // fallback: 如果没找到中文名，尝试用英文名
        if (encounterNameZh == encounterId.ToString())
        {
            foreach (var kv in EncounterNameToId)
            {
                if (kv.Value == encounterId)
                {
                    encounterNameZh = kv.Key;
                    break;
                }
            }
        }

        if (kills == 0)
        {
            specNameZh = specName;
            if (!string.IsNullOrWhiteSpace(specName) && SpecNameEnToZh.TryGetValue(specName, out var zh2))
                specNameZh = zh2;
            // encounterId中文全称
            encounterNameZh = encounterId.ToString();
            foreach (var kv in EncounterNameToId)
            {
                if (kv.Value == encounterId && kv.Key.Any(c => c > 127) && !kv.Key.Any(c => c < 128)) // 只取第一个全中文名
                {
                    encounterNameZh = kv.Key;
                    break;
                }
            }
            // fallback: 如果没找到中文名，尝试用英文名
            if (encounterNameZh == encounterId.ToString())
            {
                foreach (var kv in EncounterNameToId)
                {
                    if (kv.Value == encounterId)
                    {
                        encounterNameZh = kv.Key;
                        break;
                    }
                }
            }
            return $"{name}({specNameZh}): {encounterNameZh} 未过本";
        }

        return
            $"{name}({specNameZh}): {encounterNameZh} 最高{metric}:{(int)Math.Round(best)}，通关数:{kills}，排名百分比:{rtn:F1}%({color})";
    }

    // 新增：返回原始json
    public static async Task<string> QueryDpsRawAsync(string name, string server, int encounterId, string specName,
        string metric)
    {
        string region = "CN";
        string token = await GetAccessTokenAsync();
        var query =
            @"query($name: String!, $server: String!, $serverRegion: String!, $encounterID: Int!, $metric: CharacterRankingMetricType!, $specName: String) { characterData { character(name: $name, serverSlug: $server, serverRegion: $serverRegion) { encounterRankings(encounterID: $encounterID, metric: $metric, specName: $specName) } } }";
        string jsonPayload =
            $"{{\"query\":\"{query.Replace("\"", "\\\"")}\",\"variables\":{{\"name\":\"{name}\",\"server\":\"{server}\",\"serverRegion\":\"{region}\",\"encounterID\":{encounterId},\"metric\":\"{metric}\",\"specName\":\"{specName}\"}}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.fflogs.com/api/v2/client")
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Accept", "application/json");
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return json;
    }

    // 新增：副本名转ID映射
    public static readonly Dictionary<string, int> EncounterNameToId =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "UCoB", 1060 }, { "绝巴哈", 1060 }, { "巴哈", 1060 }, { "巴哈姆特绝境战", 1060 }, { "巴", 1060 }, { "巴哈姆特", 1060 },
            { "UwU", 1061 }, { "绝神兵", 1061 }, { "神兵", 1061 }, { "究极神兵绝境战", 1061 }, { "兵", 1061 }, { "究极神兵", 1061 },
            { "TEA", 1062 }, { "绝亚", 1062 }, { "亚历山大绝境战", 1062 }, { "绝亚历山大", 1062 }, { "亚", 1062 }, { "亚历山大", 1062 },
            { "DSR", 1065 }, { "龙诗", 1065 }, { "幻想龙诗绝境战", 1065 }, { "绝龙诗", 1065 }, { "龙", 1065 }, { "幻想龙诗", 1065 },
            { "TOP", 1068 }, { "绝欧", 1068 }, { "欧米茄绝境验证战", 1068 }, { "绝欧米茄", 1068 }, { "欧", 1068 }, { "绝O", 1068 },
            { "欧米茄", 1068 },
            { "FRU", 1079 }, { "绝伊甸", 1079 }, { "光暗未来绝境战", 1079 }, { "绝ed", 1079 }, { "伊甸", 1079 }, { "光暗未来", 1079 },
            { "M5S", 97 }, { "阿卡狄亚登天斗技场 中量级1", 97 }, { "Dancing Green", 97 }, { "热舞绿光", 97 },
            { "M6S", 98 }, { "阿卡狄亚登天斗技场 中量级2", 98 }, { "Sugar Riot", 98 }, { "狂热糖潮", 98 },
            { "M7S", 99 }, { "阿卡狄亚登天斗技场 中量级3", 99 }, { "Brute Abombinator", 99 }, { "野蛮恨心", 99 },
            { "M8S", 100 }, { "阿卡狄亚登天斗技场 中量级4", 100 }, { "Howling Blade", 100 }, { "剑嚎", 100 },
            { "极泽莲尼娅", 1080 }, { "Zelenia", 1080 },
            { "Cloud of Darkness", 2061 }, { "黑暗之云", 2061 },
            { "Black Cat", 93 }, { "Honey B. Lovely", 94 }, { "Brute Bomber", 95 }, { "Wicked Thunder", 96 },
            { "M1S", 93 }, { "阿卡狄亚登天斗技场 轻量级1", 93 }, { "黑猫", 93 },
            { "M2S", 94 }, { "阿卡狄亚登天斗技场 轻量级2", 94 }, { "蜂蜂小甜心", 94 },
            { "M3S", 95 }, { "阿卡狄亚登天斗技场 轻量级3", 95 }, { "野蛮爆弹狂人", 95 },
            { "M4S", 96 }, { "阿卡狄亚登天斗技场 轻量级4", 96 }, { "狡雷", 96 },
            { "M9S", 101 }, { "阿卡狄亚登天斗技场 重量级5", 101 },
            { "M10S", 102 }, { "阿卡狄亚登天斗技场 重量级6", 102 },
            { "M11S", 103 }, { "阿卡狄亚登天斗技场 重量级7", 103 },
            { "M12S", 104 }, { "阿卡狄亚登天斗技场 重量级8", 104 },
            { "1122", 1068 }, // 欧米茄绝境验证战_时空狭缝 → 绝欧
            { "968", 1065 },  // 幻想龙诗绝境战_诗想空间 → 绝龙诗
            { "887", 1062 },  // 亚历山大绝境战_差分闭合宇宙 → 绝亚
            { "777", 1061 },  // 究极神兵绝境战_禁绝幻想 → 绝神兵
            { "733", 1060 },  // 巴哈姆特绝境战_巴哈姆特大迷宫 → 绝巴哈
            // ...如有其它副本英文名请继续补全...
        };

    // 新增：副本名/简称/encounterId/数字 → 地图id 映射表（不可与 EncounterNameToId 混用！）
    public static readonly Dictionary<string, int> EncounterToMapId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "阿卡狄亚零式登天斗技场 中量级1", 1257 },
        { "M5S", 1257 },
        { "阿卡狄亚零式登天斗技场 中量级2", 1259 },
        { "M6S", 1259 }, 
        { "阿卡狄亚零式登天斗技场 中量级3", 1261 },
        { "M7S", 1261 },
        { "阿卡狄亚零式登天斗技场 中量级4", 1263 },
        { "M8S", 1263 },
        { "泽莲尼娅歼殛战", 1271 },
        { "泽莲尼娅", 1271 },
        { "极泽莲尼娅", 1271 },
        { "Zelenia", 1271 },
        { "极泽", 1271 },
    };

    /// <summary>
    /// 查询 sumemo.dev 最优记录最远进度，需二次请求 zone 接口获取 phase/subphase 名称（Newtonsoft.Json 版本）
    /// </summary>
    /// <param name="name">角色名</param>
    /// <param name="server">服务器名</param>
    /// <param name="encounterInput">副本ID/简称/中文名/数字</param>
    /// <returns>如：最优记录最远进度：前半 光狼</returns>
    public static async Task<string> QuerySumemoProgressAsync(string name, string server, object encounterInput)
    {
        // 1. 输入转字符串
        string input = encounterInput?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(input))
            return $"未指定副本";

        // 2. 尝试用 EncounterToMapId 映射为地图ID
        int mapId = 0;
        if (EncounterToMapId.TryGetValue(input, out var foundMapId))
        {
            mapId = foundMapId;
        }
        else if (int.TryParse(input, out var id))
        {
            // 允许直接输入地图ID
            mapId = id;
        }
        else
        {
            return $"未找到副本 {input} 对应的地图ID";
        }
        if (mapId == 0)
            return $"未找到副本 {input} 对应的地图ID";

        // 3. 第一次请求 sumemo.dev API，获取 progress
        string urlBest = $"https://api.sumemo.dev/member/{Uri.EscapeDataString(name)}@{Uri.EscapeDataString(server)}/{mapId}/best";
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            string jsonBest = await client.GetStringAsync(urlBest);
            if (string.IsNullOrWhiteSpace(jsonBest))
                return $"{name}@{server} 未查到数据";

            var objBest = Newtonsoft.Json.Linq.JObject.Parse(jsonBest);
            // 判断 clear 字段
            var clearElem = objBest["clear"];
            if (clearElem != null && clearElem.Type != Newtonsoft.Json.Linq.JTokenType.Null && (bool)clearElem)
            {
                return $"{name}@{server} {input} 已过本";
            }
            var progressElem = objBest["progress"];
            if (progressElem == null)
                return $"{name}@{server} 未查到记录";
            int phaseId = (int)progressElem["phase"];
            int subphaseId = (int)progressElem["subphase"];
            // 判断 fight_id 是否存在，无记录时直接返回
            var fightIdElem = objBest["fight_id"];
            if ((fightIdElem == null || fightIdElem.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                && phaseId == 0 && subphaseId == 0)
            {
                return $"{name}@{server} {input} 未查到记录";
            }

            // 4. 第二次请求 zone 接口，获取 phase/subphase 名称
            string urlZone = $"https://api.sumemo.dev/zone/{mapId}";
            string jsonZone = await client.GetStringAsync(urlZone);
            if (string.IsNullOrWhiteSpace(jsonZone))
                return $"{name}@{server} 未查到副本结构";
            var objZone = Newtonsoft.Json.Linq.JObject.Parse(jsonZone);
            var phasesElem = objZone["phases"] as Newtonsoft.Json.Linq.JArray;
            if (phasesElem == null)
                return $"{name}@{server} 未查到副本阶段结构";

            string phaseName = null, subphaseName = null, phaseDesc = null, subphaseDesc = null;
            foreach (var phase in phasesElem)
            {
                if ((int)phase["phase_id"] == phaseId)
                {
                    phaseName = (string)phase["name"];
                    phaseDesc = (string)phase["description"];
                    var subphases = phase["subphases"] as Newtonsoft.Json.Linq.JArray;
                    if (subphases != null)
                    {
                        foreach (var subphase in subphases)
                        {
                            if ((int)subphase["subphase_id"] == subphaseId)
                            {
                                subphaseName = (string)subphase["name"];
                                subphaseDesc = (string)subphase["description"];
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            // 5. 输出格式
            string desc = "";
            if (!string.IsNullOrWhiteSpace(subphaseDesc) && !string.IsNullOrWhiteSpace(phaseDesc))
            {
                desc = $"（{subphaseDesc}，{phaseDesc}）";
            }
            else if (!string.IsNullOrWhiteSpace(subphaseDesc))
            {
                desc = $"（{subphaseDesc}）";
            }
            else if (!string.IsNullOrWhiteSpace(phaseDesc))
            {
                desc = $"（{phaseDesc}）";
            }
            if (phaseName != null && subphaseName != null)
            {
                return $"{name}@{server} {input} 最远进度：{phaseName} {subphaseName}{desc}";
            }
            else if (phaseName != null)
            {
                return $"{name}@{server} {input} 最远进度：{phaseName}{desc}";
            }
            else
            {
                return $"{name}@{server} {input} 未查到记录";
            }
        }
    }
};
    

// 注册sumemo.dev最远进度查询具名回调
