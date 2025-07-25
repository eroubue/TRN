using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Triggernometry;
using Triggernometry.PluginBridges;
using static Triggernometry.Action.ActionTypeEnum;
using static Triggernometry.Entity.RoleType;
using static Triggernometry.Interpreter.StaticHelpers;
using static Triggernometry.RealPlugin;
using Action = Triggernometry.Action;

plug.UnregisterNamedCallback("LatihasTUW");
plug.RegisterNamedCallback("LatihasTUW", new Action<object, string>(LatihasTUW.Callback), null);
 

public static class LatihasTUW {
 private static string MyJob, MyName, hs, ttsafe, hsadd;
 private static bool Ubroadcast, Uthreebucket,Uauto,Umarklocal,Umark,Utarget;
 private static Vector2 Zhuzi2;
 private static Ciyu ciyu;
 private static List<DHP43> p43dhDist = new();
 private static int p43dhCount;
 private static readonly Player[] Players = new Player[8];
 private static readonly List<string> P43PlayerName = new();
 private static readonly List<Player> Threebuckets = new();
 private static readonly Trigger _tri = new();
 private static readonly List<Zhuzi> Zhuzis = new();
 private static readonly string[] JobOrder = { "MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4" },
  TBJobOrder = { "MT", "ST", "D1", "D2", "D3", "D4", "H1", "H2" },
  Server = {
   "红玉海", "神意之地", "拉诺西亚", "幻影群岛", "萌芽池", "宇宙和音", "沃仙曦染", "晨曦王座", "白银乡", "白金幻象", "神拳痕", "潮风亭", "旅人栈桥", "拂晓之间",
   "龙巢神殿", "梦羽宝境", "紫水栈桥", "延夏", "静语庄园", "摩杜纳", "海猫茶屋", "柔风海湾", "琥珀原", "水晶塔", "银泪湖", "伊修加德", "太阳海岸", "红茶川", "Anima",
   "Belias", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium", "Shinryu", "Titan", "Asura"
  };

 private static void Place(string expr) {
  try {
   var sb = new StringBuilder("{");
   foreach (var s in expr.Split(';')) {
    var parts = s.Split(':');
    var name = parts[0] switch {
     "1" => "One",
     "2" => "Two",
     "3" => "Three",
     "4" => "Four",
     _ => parts[0]
    };
    if (parts[1] == "clear")
     sb.Append($"\"{name}\":{{}},");
    else {
     var xy = parts[1].Split(',');
     sb.Append($"\"{name}\":{{\"X\":{xy[0]},\"Z\":{xy[1]},\"Y\":0,\"Active\":true}},");
    }
   }
   plug.InvokeNamedCallback("place", sb.Append("}").ToString());
  }
  catch (Exception ex) {
   Log($"Place Error({expr}):{ex.StackTrace}");
  }
 }

 public static void Callback(object _, string str) {
  try {
   var ss = str.Split(':');
   switch (ss[0].ToLower()) {
    case "initplace": //v
     Place(StaticPlace.initPlace);
     break;
    case "broadcast": //str
     Broadcast(ss[1]);
     break;
    case "tts": //str
     PostTTS(ss[1]);
     break;
    case "ydsx":
     ydsx();
     break;
    case "Target":
     Target(ss[1]);
     break;
    case "rf": //str:name
     rf(ss[1]);
     break;
    case "xfzd": //time
     xfzd(ss[1]);
     break;
    case "clear2": //time
     Place(StaticPlace.clear2);
     break;
    case "death": //id
     Death(ss[1]);
     break;
    case "p0init": //str:job
     if (str.Contains(":")) {
      var tmp = ss[1].Split(',')[0].ToUpper();
      if (JobOrder.All(j => j != tmp)) {
       if (tmp == Tank.ToString().ToUpper()) MyJob = "MT";
       if (tmp == PureHealer.ToString().ToUpper()) MyJob = "H1";
       if (tmp == BarrierHealer.ToString().ToUpper()) MyJob = "H2";
       if (tmp == StrengthMelee.ToString().ToUpper()) MyJob = "D1";
       if (tmp == DexterityMelee.ToString().ToUpper()) MyJob = "D2";
       if (tmp == PhysicalRanged.ToString().ToUpper()) MyJob = "D3";
       if (tmp == MagicalRanged.ToString().ToUpper()) MyJob = "D4";
      }
      else MyJob = tmp;
      GetPartyOrderInit(ss[1]);
     }
     else if (MyJob != null) {
      InitParams();
      PostTip($"已使用{MyJob}进行初始化");
     }
     break;
    case "p1place": //void
     InitParams();
     Place(StaticPlace.initPlace);
     Place(StaticPlace.clear2);
     Place(StaticPlace.clear3);
     Place(StaticPlace.clear4);
     if (MyJob == null) PostTTS("未设置职业，请检查设置。");
     if (Ubroadcast) Broadcast("已开启团队播报，请注意不要冲突");
     if (Uthreebucket) Broadcast("已开启三桶播报，请注意不要冲突");
     if (Uauto)Broadcast("已开启全自动打印移动坐标，如不需要使用请关闭。");
     if (Umark&&Umarklocal)Broadcast("已启用本地标记。");
     if (Umark&&!Umarklocal)Broadcast("已启用小队可见标记。");
     
     break;
    case "p1ciyu": //str
     lock (ciyu) { ciyu.zid = ss[1]; }
     break;
    case "p1ciyudmg": //jid,desc
     var sx = ss[1].Split(',');
     if (sx[0] is "2B45" or "2B46") break;
     lock (ciyu) { ciyu.dmg.Add(sx[1]); }
     break;
    case "p1ciyucs": //void
     lock (ciyu) { ciyu.cs2 = true; }
     break;
    case "p1fshp1": //void
     p1fshp();
     break;
    case "p1fshp2": //void
     p1fshp(false);
     break;
    case "p1fs1": //void
     var es = GetXYFromBnpcid("8723");
     if (es.Count == 2) {
      var fs1 = GetDir(es[0]);
      var fs2 = GetDir(es[1]);
      PostTTS($"{fs1},{fs2}");
      FsPlaceByRule(fs1, fs2);
     }
     break;
    case "p1fs2": //void
     const string fs3 = "左西";
     const string fs4 = "右东";
     PostTTS($"{fs3},{fs4}");
     FsPlace(fs3, fs4);
     break;
    case "p2hs": //id,x,y
     p2hs(ss[1]);
     break;
    case "p2safe": //x,y
     p2safe(ss[1]);
     break;
    case "p2zhuzi": //id,x,y
     p2zhuzi(ss[1]);
     break;
    case "p2zhuzidmg": //jid,desc
     p2zhuzidmg(ss[1]);
     break;
    case "p2zhuzijx": //id
     p2zhuzijx(ss[1]);
     break;
    case "p2zhuzi23": //vois
     p2zhuzi23();
     break;
    case "p2hsjx": //id
     p2hsjx(ss[1]);
     break;
    case "p3threebucket": //name
     ThreeBucket(ss[1]);
     break;
    case "p3fly": //id
     p3fly(ss[1]);
     break;
    case "p3ygjd": //x,y
     NEWp3ygjd(ss[1]);
     break;
    case "p3nl": //id
     p3nl(ss[1]);
     break;
    case "p3lb":
     p3lb();
     break;
    case "p41": //void
     p41();
     break;
    case "p41taitan": //x
     p41taitan(ss[1]);
     break;
    case "p41place2": //void
     Place(StaticPlace.p41place2);
     break;
    case "p42hs": //void
     p42hs();
     break;
    case "p42place2": //void
     Place(StaticPlace.p42place2);
     break;
    case "p42place4d3": //void
     Place(StaticPlace.p42place4d3);
     break;
    case "p42place2rf": //void
     Place(StaticPlace.p42place2rf);
     break;
    case "p43place": //void
     Place(StaticPlace.startp43);
     break;
    case "p43dh": //xy
     p43dh(ss[1]);
     break;
    case "p43fq": //name
     p43dh_fq(ss[1], "风枪");
     break;
    case "p43qdptrace": //xy
     p43qdptrace(ss[1]);
     break;
    case "jzha": //void
     Place(StaticPlace.p5jzhA);
     Broadcast("A点", true);
     break;
    case "jzh2": //void
     Place(StaticPlace.p5jzh2);
     Broadcast("2点", true);
     break;
    case "jzh3": //void
     Place(StaticPlace.p5jzh3);
     Broadcast("3点", true);
     break;
   }
  }
  catch (Exception e) {
   Log($"Error: {str}");
   Log(e.StackTrace);
  }
 }

 private static void p2hsjx(string id) {
  var e = BridgeFFXIV.GetIdEntity(id);
  if (e.GetValue("bnpcid").ToString() == "8730") hsadd = e.GetValue("address").ToString();
 }

 private static void p3lb() {
  BelowHP("8727", 0.15, "请注意土神血量和LB槽", "stop2");
 }
 
 private static void markerBelowHPtarget(string marker, double percent, string hexid) {
  foreach (var en in BridgeFFXIV.GetAllEntities().Where(x => x.GetValue("Marker").ToString() == marker)) {
   bool isLowHp = 1.0 * int.Parse(en.GetValue("currenthp").ToString()) / int.Parse(en.GetValue("maxhp").ToString()) < percent;
   bool hasLowStack = int.Parse(en.GetValue("StatusStack(609)").ToString()) <= 1;
   if (!(isLowHp && hasLowStack)) return;
   Target(hexid); 
   return;
  }
 }
 
 /*private static void p2FindTargetzhuzi()
 {
  if (MyJob is not ("D1" or "D2" or "D3" or "D4"))
   return;
  string suffix = MyJob.Substring(1); 
  string targetMark = $"attack{suffix}";
  foreach (var entity in BridgeFFXIV.GetAllEntities())
  {
   var markType = entity?.GetValue("Marker")?.ToString();
   if (markType == targetMark)
   {
    var hexId = entity.GetValue("id");
    Target(hexId);
    PostTip($"已切换至 {targetMark} 标记的目标");
    string hex火神Id = FindTargetHexIdByBnpcId("8730");
    markerBelowHPtarget(targetMark, 0.4, hex火神Id);
    return;
   }
  }
  PostTip($"{targetMark} 标记未找到");
 }*/
 private static void Newp2FindTargetzhuzi(int a1, int a2, int a3, int a4)
 {
  if (MyJob is not ("D1" or "D2" or "D3" or "D4"))
   return;

  // 根据传入的 a1, a2, a3, a4 和 job 判断 Zhuzis 的索引
  int targetIndex = -1;
  switch (MyJob)
  {
   case "D1":
    targetIndex = a1;
    break;
   case "D2":
    targetIndex = a2;
    break;
   case "D3":
    targetIndex = a3;
    break;
   case "D4":
    targetIndex = a4;
    break;
  }

  if (targetIndex == -1 || targetIndex >= Zhuzis.Count)
   return;
  var targetZid = Zhuzis[targetIndex].zid;
  Target(targetZid);
  PostTip($"已切换目标");
  string hex火神Id = FindTargetHexIdByBnpcId("8730");
  markerBelowHPtarget(targetZid, 0.4, hex火神Id);
 
 }



 private static void BelowHP(string bnpcid, double percent, string desc, string marktype) {
  foreach (var en in BridgeFFXIV.GetAllEntities().Where(x => x.GetValue("bnpcid").ToString() == bnpcid)) {
   if (!(1.0 * int.Parse(en.GetValue("currenthp").ToString()) / int.Parse(en.GetValue("maxhp").ToString()) < percent)) return;
   Broadcast(desc, true);
   Mark(en.GetValue("id"), marktype);
   return;
  }
 }

 private static void Mark(object hexId, string marktype) {
  if (Umarklocal&&Umark) plug.InvokeNamedCallback("mark", $"{{\"ActorID\":0x{hexId},\"MarkType\":\"{marktype}\",\"LocalOnly\":\"True\"}}");
  if (!Umarklocal&&Umark) plug.InvokeNamedCallback("mark", $"{{\"ActorID\":0x{hexId},\"MarkType\":\"{marktype}\"}}");
 }
 private static void Target(object hexId) {
  if(Utarget)plug.InvokeNamedCallback("Target", $"{{\"ActorID\":0x{hexId}}}");
 }

 private static string FindTargetHexIdByBnpcId(string targetBnpcId)
 {
  foreach (var entity in BridgeFFXIV.GetAllEntities())
  {
   var bnpcid = entity.GetValue("bnpcid")?.ToString();
   var isTargetable = entity.GetValue("IsTargetable")?.ToString();

   if (bnpcid == targetBnpcId && isTargetable == "1")
   {
    return entity.GetValue("id")?.ToString(); // 返回 hexid
   }
  }

  return null; // 未找到符合条件的实体
 }

 private static void p1fshp(bool first = true) {
  if (first) BelowHP("8722", 0.1, "请注意风神血量", "stop1");
  else BelowHP("8722", 0.05, "请注意风神血量", "stop2");
 }

 private static void Death(string id) {
  foreach (var z in Zhuzis.Where(z => z.zid == id && !z.cs2)) {
   Broadcast($"柱炸:{string.Join("", z.dmg)}");
   break;
  }
  lock (ciyu) {
   if (id == ciyu.zid && !ciyu.cs2) Broadcast($"羽炸:{string.Join("", ciyu.dmg)}");
  }
 }

 private static void p43qdptrace(string xy) {
  var pos = GetXY(xy);
  Place($"2:{pos.X},{pos.Y}");
  if (pos is { X: > 100, Y: > 100 }) Broadcast("警告，潜地炮位置出现在右下");
 }

 private static void xfzd(string time) {
  Task.Run(async delegate {
   await Task.Delay(((int)float.Parse(time) - 3) * 1000);
   PostTTS("准备爆炸");
  });
 }

 private static void p3nl(string id) {
  Broadcast("优先攻击石牢");
  Mark(id, "attack1");
 }

 private static void rf(string name) {
  var HDead = false;
  foreach (var p in Players) {
   if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is "H1" or "H2") {
    Log("Dead: " + p.name);
    HDead = true;
   }
   if (p.name != name || MyName != name) continue;
   PostTip("热风点你，快出去！");
  }
  if (HDead) Broadcast("奶死亡，热风注意出人群", true);
 }

 private static void ydsx() {
  var TDead = false;
  foreach (var p in Players) {
   if (BridgeFFXIV.GetNamedPartyMember(p.name).GetValue("currenthp").ToString() == "0" && p.job is "ST" or "MT") {
    Log("Dead: " + p.name);
    TDead = true;
   }
  }
  var op = "二仇炮。";
  if (TDead) {
   op += "T死亡，二仇注意出人群";
   Broadcast(op, true);
  }
  PostTTS(op);
 }

 private static void p42hs() {
  var yflt = GetXYFromBnpcid("8730", 1)[0];
  var dir = GetDir(yflt);
  if (dir is "左上西北" or "右下东南") Place("4:112,112");
  if (dir is "左下西南" or "右上东北") Place("4:88,112");
 }

 private static void p41() {
  var jll = GetXYFromBnpcid("8722", 1)[0];
  var tt = GetXYFromBnpcid("8727", 1)[0];
  var yflt = GetXYFromBnpcid("8730", 1)[0];
  var yflt_dir = GetDir(yflt);
  var jjsb = GetXYFromBnpcid("8734")[0];
  var jjsb_dir = GetDir(jjsb);
  var tt_dir = GetDir(tt);
  var result = new List<P41Info>();
  foreach (var s in new[] { "上北", "下南", "左西", "右东" }) {
   if (jll.X > 100 && s == "右东" || jll.X < 100 && s == "左西" ||
    jll.Y > 100 && s == "下南" || jll.Y < 100 && s == "上北" ||
    s == tt_dir) continue;
   switch (s) {
    case "上北":
     if (jjsb_dir != "左上西北")
      result.Add(new P41Info(s, "上北然后逆时针", new Vector2(91, 83),
       yflt_dir != "左上西北" && yflt_dir != "右下东南"));
     if (jjsb_dir != "右上东北")
      result.Add(new P41Info(s, "上北然后顺时针", new Vector2(109, 83),
       yflt_dir != "右上东北" && yflt_dir != "左下西南"));
     break;
    case "下南":
     if (jjsb_dir != "左下西南")
      result.Add(new P41Info(s, "下南然后顺时针", new Vector2(91, 117),
       yflt_dir != "右上东北" && yflt_dir != "左下西南"));
     if (jjsb_dir != "右下东南")
      result.Add(new P41Info(s, "下南然后逆时针", new Vector2(109, 117),
       yflt_dir != "左上西北" && yflt_dir != "右下东南"));
     break;
    case "左西":
     if (jjsb_dir != "左上西北")
      result.Add(new P41Info(s, "左西然后顺时针", new Vector2(83, 91),
       yflt_dir != "左上西北" && yflt_dir != "右下东南"));
     if (jjsb_dir != "左下西南")
      result.Add(new P41Info(s, "左西然后逆时针", new Vector2(83, 109),
       yflt_dir != "右上东北" && yflt_dir != "左下西南"));
     break;
    case "右东":
     if (jjsb_dir != "右上东北")
      result.Add(new P41Info(s, "右东然后逆时针", new Vector2(117, 91),
       yflt_dir != "右上东北" && yflt_dir != "左下西南"));
     if (jjsb_dir != "右下东南")
      result.Add(new P41Info(s, "右东然后顺时针", new Vector2(117, 109),
       yflt_dir != "左上西北" && yflt_dir != "右下东南"));
     break;
   }
  }
  var esresult = result.Where(t => t.canES).ToList();
  esresult.AddRange(result.Where(t => !t.canES));
  var recommand = esresult[0];
  PostTip(recommand.desc);
  Place(recommand.first switch {
   "上北" => StaticPlace.safeA,
   "下南" => StaticPlace.safeC,
   "左西" => StaticPlace.safeD,
   "右东" => StaticPlace.safeB
  });
  Log($"可能安全点:{string.Join("|", esresult)}。神兵:{jjsb_dir},土神:{tt_dir},火神:{yflt_dir}。");
  Task.Run(async delegate {
   await Task.Delay(recommand.canES ? 1000 : 6000);
   Place($"4:{recommand.after.X},{recommand.after.Y}");
  });
 }

 private static void p43dh_fq(string s, string desc) {
  if (s == MyName) PostTip(desc);
  else if (desc == "地火") desc += "(仅供参考)";
  lock (P43PlayerName) {
   P43PlayerName.Add(s);
   Broadcast($"{desc} {s}");
   if (P43PlayerName.Count == 5) p43qdp();
  }
 }

 private static void p43qdp() {
  var nm = "";
  foreach (var player in Players) {
   var pn = player.name;
   if (player.job is "MT" or "ST" || P43PlayerName.Contains(pn)) continue;
   if (nm == "") nm = pn;
   else return;
  }
  if (nm == MyName) PostTip("潜地炮");
  Broadcast($"潜地炮 {nm}");
 }

 private static void p43dh(string xy) {
  var pos = GetXY(xy);
  lock (p43dhDist) {
   var iter = 0;
   foreach (var player in Players) {
    if (player.job is "ST" or "MT") continue;
    var pn = player.name;
    var pl = BridgeFFXIV.GetNamedPartyMember(pn);
    var dist = Vector2.DistanceSquared(pos,
     new Vector2(float.Parse(pl.GetValue("x").ToString()),
      float.Parse(pl.GetValue("y").ToString())));
    if (p43dhCount == 0) p43dhDist.Add(new DHP43(pn, dist));
    else {
     p43dhDist[iter].dist = Math.Min(p43dhDist[iter].dist, dist);
     iter++;
    }
   }
   if (++p43dhCount != 3) return;
   p43dhDist.Sort((a, b) => a.dist.CompareTo(b.dist));
   p43dh_fq(p43dhDist[0].name, "地火");
   p43dh_fq(p43dhDist[1].name, "地火");
   p43dh_fq(p43dhDist[2].name, "地火");
  }
 }

 private static void p41taitan(string x) {
  var dir = x switch {
   "113.70" => "右右右",
   "86.30" => "左左左"
  };
  PostTip(dir);
 }

 private static void p2zhuzijx(string s) {
  lock (Zhuzis) {
   foreach (var z in Zhuzis.Where(z => z.zid == s)) {
    z.cs2 = true;
    break;
   }
  }
 }


 private static void p2zhuzidmg(string st) {
  var ss = st.Split(',');
  if (ss[0] == "2B58") return;
  lock (Zhuzis) {
   foreach (var z in Zhuzis.Where(z => z.zid == ss[2])) {
    z.dmg.Add(ss[1]);
    break;
   }
  }
 }

 private static void p2zhuzi23() {
  if (Zhuzi2.X is 83.88f or 93.3f) Zhuzi2.X = 88;
  if (Zhuzi2.X is 106.7f or 116.12f) Zhuzi2.X = 112;
  if (Zhuzi2.Y is 83.88f or 93.3f) Zhuzi2.Y = 88;
  if (Zhuzi2.Y is 106.7f or 116.12f) Zhuzi2.Y = 112;
  Place($"2:{Zhuzi2.X},{Zhuzi2.Y};3:{200 - Zhuzi2.X},{200 - Zhuzi2.Y}");
 }

 private static void p2zhuzimark(int a1, int a2, int a3, int a4) {
  Mark(Zhuzis[a1].zid, "attack1");
  Mark(Zhuzis[a2].zid, "attack2");
  Mark(Zhuzis[a3].zid, "attack3");
  Mark(Zhuzis[a4].zid, "attack4");
  plug.InvokeNamedCallback("command",$"/e 柱子标记完成");
  
  
 }
 
 private static void FindIntersectionWithCircle(int a1,int a2,int a3, int a4)
 {
  // 步骤 1: 检查索引是否合法
  if (Zhuzis.Count <= a3 || Zhuzis.Count <= a4)
  {
   Console.WriteLine("索引超出范围");
   return;
  }

  // 步骤 2: 获取 pos 值
  int posA1 = Zhuzis[a1].pos;
  int posA2 = Zhuzis[a2].pos;
  int posA3 = Zhuzis[a3].pos;
  int posA4 = Zhuzis[a4].pos;

  // 假设 pos 是一个二维点，比如通过 DecodePos 方法解析
  Vector2 pointA1 = DecodePos(posA1);
  Vector2 pointA2 = DecodePos(posA2);
  Vector2 pointA3 = DecodePos(posA3); // 需要定义 DecodePos 方法
  Vector2 pointA4 = DecodePos(posA4);

  // 步骤 3: 计算中点 zdpos
  Vector2 zdpos = new Vector2(
   (pointA3.X + pointA4.X) / 2,
   (pointA3.Y + pointA4.Y) / 2
  );

  // 圆心和半径
  Vector2 center = new Vector2(100, 100);
  float radius = 15;

  // 步骤 4: 计算从圆心到 zdpos 的方向向量
  Vector2 direction = Vector2.Normalize(zdpos - center);
  // 计算方向向量并归一化
  Vector2 D1direction = Vector2.Normalize(pointA1 - center);
  Vector2 D2direction = Vector2.Normalize(pointA2 - center);

  // 沿方向移动 7 单位长度
  Vector2 D1resultPoint = center + D1direction * 7;
  Vector2 D2resultPoint = center + D2direction * 7;

  // 步骤 5: 射线与圆的交点（沿方向移动半径长度）
  Vector2 intersection = center + direction * radius;

  // 步骤 6: 输出交点
  RotateIntersection(intersection);
  plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {intersection.X}, {intersection.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
  plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 10\nPos: {D1resultPoint.X}, {D1resultPoint.Y}\nScale: 1,1\nColor: 0.1, 0.1, 1, 0.9");
  plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 10\nPos: {D2resultPoint.X}, {D2resultPoint.Y}\nScale: 1,1\nColor: 0.1, 0.1, 1, 0.9");
  
  if (Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D1坐标：({D1resultPoint.X}, {D1resultPoint.Y})");
  if (Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D2坐标：({D2resultPoint.X}, {D2resultPoint.Y})");
  if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火第四次移动坐标：({intersection.X}, {intersection.Y})");
 }

// 示例 DecodePos 方法（根据实际数据调整）
 private static Vector2 DecodePos(int pos)
 {
  // 这里假设每个 pos 对应一个预定义的坐标
  switch (pos)
  {
   case 1 << 7: return new Vector2(100, 90);
   case 1 << 6: return new Vector2(107, 93);
   case 1 << 5: return new Vector2(110, 100);
   case 1 << 4: return new Vector2(107, 107);
   case 1 << 3: return new Vector2(100, 110);
   case 1 << 2: return new Vector2(93, 107);
   case 1 << 1: return new Vector2(90, 100);
   case 1: return new Vector2(93, 93);
   default: return new Vector2(100, 100); // 默认值
  }
 }
private static void RotateIntersection(Vector2 intersection)
{
    // 圆心
    Vector2 center = new Vector2(100, 100);

    // 自定义角度转弧度函数
    float ToRadians(float degrees) => (float)(degrees * Math.PI / 180.0);

    // 逆时针旋转
    

    // 第一次旋转：40度
    Vector2 translatedPoint = intersection - center;
    Vector2 rotatedPoint1 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(40)) - translatedPoint.Y * (float)Math.Sin(ToRadians(40)),
        translatedPoint.X * (float)Math.Sin(ToRadians(40)) + translatedPoint.Y * (float)Math.Cos(ToRadians(40))
    );
    Vector2 finalPoint1 = rotatedPoint1 + center;
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint1.X}, {finalPoint1.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D4第三次移动坐标：({finalPoint1.X}, {finalPoint1.Y})");

    // 第二次旋转：45度
    translatedPoint = finalPoint1 - center;
    Vector2 rotatedPoint2 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(45)) - translatedPoint.Y * (float)Math.Sin(ToRadians(45)),
        translatedPoint.X * (float)Math.Sin(ToRadians(45)) + translatedPoint.Y * (float)Math.Cos(ToRadians(45))
    );
    Vector2 finalPoint2 = rotatedPoint2 + center;
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint2.X}, {finalPoint2.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D4第二次移动坐标：({finalPoint2.X}, {finalPoint2.Y})");

    // 第三次旋转：33度
    translatedPoint = finalPoint2 - center;
    Vector2 rotatedPoint3 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(33)) - translatedPoint.Y * (float)Math.Sin(ToRadians(33)),
        translatedPoint.X * (float)Math.Sin(ToRadians(33)) + translatedPoint.Y * (float)Math.Cos(ToRadians(33))
    );
    Vector2 finalPoint3 = rotatedPoint3 + center;
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint3.X}, {finalPoint3.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D4第一次坐标：({finalPoint3.X}, {finalPoint3.Y})");


    // 顺时针旋转
    Console.WriteLine("\n顺时针旋转：");

    // 第一次旋转：-40度
    translatedPoint = intersection - center;
    rotatedPoint1 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(-40)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-40)),
        translatedPoint.X * (float)Math.Sin(ToRadians(-40)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-40))
    );
    finalPoint1 = rotatedPoint1 + center;
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint1.X}, {finalPoint1.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D3第三次移动坐标：({finalPoint1.X}, {finalPoint1.Y})");

    // 第二次旋转：-45度
    translatedPoint = finalPoint1 - center;
    rotatedPoint2 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(-45)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-45)),
        translatedPoint.X * (float)Math.Sin(ToRadians(-45)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-45))
    );
    finalPoint2 = rotatedPoint2 + center;
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D3第二次移动坐标：({finalPoint2.X}, {finalPoint2.Y})");
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint2.X}, {finalPoint2.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");

    // 第三次旋转：-33度
    translatedPoint = finalPoint2 - center;
    rotatedPoint3 = new Vector2(
        translatedPoint.X * (float)Math.Cos(ToRadians(-33)) - translatedPoint.Y * (float)Math.Sin(ToRadians(-33)),
        translatedPoint.X * (float)Math.Sin(ToRadians(-33)) + translatedPoint.Y * (float)Math.Cos(ToRadians(-33))
    );
    finalPoint3 = rotatedPoint3 + center;
 
    plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 20\nPos: {finalPoint3.X}, {finalPoint3.Y}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
    if(Uauto)plug.InvokeNamedCallback("Command" ,$"/e 地火D3第一次移动坐标：({finalPoint3.X}, {finalPoint3.Y})");
}



 private static void p2zhuzi(string dxy) {
  var ss = dxy.Split(',');
  var d = ss[0];
  var x = ss[1];
  var y = ss[2];
  lock (Zhuzis) {
   if (x == "100.00" && y == "90.00") Zhuzis.Add(new Zhuzi(d, 1 << 7));
   else if (x == "107.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1 << 6));
   else if (x == "110.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 5));
   else if (x == "107.00" && y == "107.00") Zhuzis.Add(new Zhuzi(d, 1 << 4));
   else if (x == "100.00" && y == "110.00") Zhuzis.Add(new Zhuzi(d, 1 << 3));
   else if (x == "93.00" && y is "107.00" or "106.95" or "107") Zhuzis.Add(new Zhuzi(d, 1 << 2));
   else if (x == "90.00" && y == "100.00") Zhuzis.Add(new Zhuzi(d, 1 << 1));
   else if (x == "93.00" && y == "93.00") Zhuzis.Add(new Zhuzi(d, 1));
   else Log($"Error: Zhuzi {dxy}.");

   if (Zhuzis.Count != 4) return;
   Zhuzis.Sort((a, b) => a.pos.CompareTo(b.pos));
   switch (Zhuzis.Aggregate(0, (current, z) => current | z.pos)) {
    case 0b11010010:
     Zhuzi2 = new Vector2(93.3f, 116.12f);
     p2zhuzimark(1, 0, 2, 3);
     Newp2FindTargetzhuzi(1, 0, 2, 3);
     FindIntersectionWithCircle(1,0,2,3);
     break;
    case 0b01101001:
     Zhuzi2 = new Vector2(83.88f, 106.7f);
     p2zhuzimark(1, 0, 2, 3);
     Newp2FindTargetzhuzi(1, 0, 2, 3);
     FindIntersectionWithCircle(1,0,2,3);
     break;
    case 0b10110100:
     Zhuzi2 = new Vector2(83.88f, 93.3f);
     p2zhuzimark(0, 3, 1, 2);
     Newp2FindTargetzhuzi(0, 3, 1, 2);
     FindIntersectionWithCircle(0,3,1,2);
     break;
    case 0b01011010:
     Zhuzi2 = new Vector2(93.3f, 83.88f);
     p2zhuzimark(0, 3, 1, 2);
     Newp2FindTargetzhuzi(0, 3, 1, 2);
     FindIntersectionWithCircle(0,3,1,2);
     break;
    case 0b00101101:
     Zhuzi2 = new Vector2(106.7f, 83.88f);
     p2zhuzimark(0, 3, 1, 2);
     Newp2FindTargetzhuzi(0, 3, 1, 2);
     FindIntersectionWithCircle(0,3,1,2);
     break;
    case 0b10010110:
     Zhuzi2 = new Vector2(116.12f, 93.3f);
     p2zhuzimark(3, 2, 0, 1);
     Newp2FindTargetzhuzi(3, 2, 0, 1);
     FindIntersectionWithCircle(3,2,0,1);
     break;
    case 0b01001011:
     Zhuzi2 = new Vector2(116.12f, 106.7f);
     p2zhuzimark(3, 2, 0, 1);
     Newp2FindTargetzhuzi(3, 2, 0, 1);
     FindIntersectionWithCircle(3,2,0,1);
     break;
    case 0b10100101:
     Zhuzi2 = new Vector2(106.7f, 116.12f);
     p2zhuzimark(2, 1, 3, 0);
     Newp2FindTargetzhuzi(2, 1, 3, 0);
     FindIntersectionWithCircle(2,1,3,0);
     break;
   }
   Place($"2:{Zhuzi2.X},{Zhuzi2.Y}");
  }
 }
 private static void NEWp3ygjd(string xy)
{
    // 输入验证
    var ss = xy.Split(',');
    if (ss.Length != 2 || string.IsNullOrEmpty(ss[0]) || string.IsNullOrEmpty(ss[1]))
        return;

    var x = ss[0];
    var y = ss[1];

    // 定义方向映射
    string ygjddir = "";
    bool isRight = (x == "95.00" && (y == "111.00" || y == "112.00")) ||
                   (x == "88.00" && y == "95.00") ||
                   (x == "105.00" && y == "88.00") ||
                   (x == "112.00" && y == "105.00");

    bool isLeft = (x == "105.00" && y == "112.00") ||
                  (x == "88.00" && y == "105.00") ||
                  (x == "95.00" && y == "88.00") ||
                  ((x == "111.00" || x == "112.00") && y == "95.00");

    if (isRight) ygjddir = "右右右";
    else if (isLeft) ygjddir = "左左左";
    else return;

    // 定义坐标映射表
    var placeMap = new Dictionary<(string, string), string>
    {
        { ("95.00", "111.00"), "3:105,105" },//A右3
        { ("95.00", "112.00"), "3:105,105" },
        { ("111.00", "95.00"), "3:105,105" },//D左3
        { ("112.00", "95.00"), "3:105,105" },

        { ("105.00", "112.00"), "3:95,105" },//A左3
        { ("88.00", "95.00"), "3:95,105" },

        { ("105.00", "88.00"), "3:95,95" },
        { ("88.00", "105.00"), "3:95,95" },

        { ("95.00", "88.00"), "3:105,95" },
        { ("112.00", "105.00"), "3:105,95" }//D右3
    };

    if (placeMap.TryGetValue((x, y), out var place))
    {
        Place(place);
        var posValue = place.Split(new[] { ':' }, 2).LastOrDefault();
        plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f\nt: 2\nPos: {posValue}\nScale: 1,1\nColor: 0.1, 1, 0.1, 0.9");
        if (Uauto) plug.InvokeNamedCallback("command", $"/e 三桶移动坐标:{posValue}");
    }

    // ttsafe 分支逻辑
    int? index = null;
    switch (ttsafe)
    {
        case "AAAA":
            Place("2:100,106");
            plug.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2\n Pos: 100,106\n Scale: 1,1\n Color: 0.1, 1, 0.1, 0.9");
            index = 0;
            if(Uauto)plug.InvokeNamedCallback("command","/e 一桶移动坐标:100,106");
            break;
        case "BBBB":
            Place("2:94,100");//D1
            plug.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos: 94,100 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
            index = 1;
            if(Uauto)plug.InvokeNamedCallback("command","/e 一桶移动坐标:94,100");
            break;
        case "CCCC":
            Place("2:100,94");//A1
            plug.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos: 100,94 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
            index = 2;
            if(Uauto)plug.InvokeNamedCallback("command","/e 一桶移动坐标:100,94");
            break;
        case "DDDD":
            Place("2:106,100");
            plug.InvokeNamedCallback("PictoACT", "Omen: er_general_1f \nt: 2 \nPos:106,100 \nScale: 1,1 \nColor: 0.1, 1, 0.1, 0.9");
            index = 3;
            if(Uauto)plug.InvokeNamedCallback("command","/e 一桶移动坐标:106,100");
            
            break;
    }

    if (index.HasValue)
    {
     string place4 = StaticPlace.ygjdPlace4[index.Value, ygjddir == "左左左" ? 0 : 1];
     Place(place4);
        
     // 提取 "4:xxx,yyy" 中冒号后的内容作为 Pos 值
     var posValue = place4.Split(new[] { ':' }, 2).LastOrDefault();
     plug.InvokeNamedCallback("PictoACT", $"Omen: er_general_1f \nt: 4.5 \nPos: {posValue} \nScale: 2,2 \nColor: 0.1, 1, 0.1, 0.9");
     if(Uauto)plug.InvokeNamedCallback("command", $"/e 大怒震移动坐标{posValue} ");
    }

    PostTip(ygjddir);
}
 
 private static void Broadcast(string s, bool tip = false) {
  if (tip) PostTip(s, tts: false);
  plug.InvokeNamedCallback("command", Ubroadcast ? $"/p {s} " : $"/e {s}");
 }

 private static string GetPScale(string name) {
  return GetScalarVariable(false, "ptuw_" + name);
 }

 private static string GetScale(string name) {
  return GetScalarVariable(false, "tuw_" + name);
 }

 private static void p3fly(string id) {
  var pos = GetXY(id);
  ttsafe = "";
  if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 114) <= 2) {
   ttsafe = "AAAA";
   Place(StaticPlace.safeA);
  }
  if (Math.Abs(pos.X - 86) <= 2 && Math.Abs(pos.Y - 100) <= 2) {
   ttsafe = "BBBB";
   Place(StaticPlace.safeB);
  }
  if (Math.Abs(pos.X - 100) <= 2 && Math.Abs(pos.Y - 86) <= 2) {
   ttsafe = "CCCC";
   Place(StaticPlace.safeC);
  }
  if (Math.Abs(pos.X - 114) <= 2 && Math.Abs(pos.Y - 100) <= 2) {
   ttsafe = "DDDD";
   Place(StaticPlace.safeD);
  }
  if (ttsafe == "") Log($"Taitan Loc Fail At:({pos.X},{pos.Y})");
  Broadcast(ttsafe, true);
  PostTTS(ttsafe);
 }

 private static void PostTTS(string text) {
  plug.QueueAction(fakectx, _tri, null,
   new Action {
    ActionType = LogMessage.ToString(),
    LogMessageText = $"[Latihas TTS] {text}",
    LogProcess = "true"
   }, DateTime.Now, true);
 }

 private static void PostTip(string text, float x = 0, float y = 0, string id = "main", bool tts = true) {
  plug.QueueAction(fakectx, _tri, null,
   new Action {
    ActionType = LogMessage.ToString(),
    LogMessageText = $"[Latihas Tip] {text}:{x}:{y}:{id}",
    LogProcess = "true"
   }, DateTime.Now, true);
  if (tts) PostTTS(text);
 }

 private static void p2safe(string xy) {
  if (hs == "") return;
  var zzpos = GetXY(xy);
  string safe = null;
  if (Math.Abs(zzpos.X - 100) < 1 && zzpos.Y < 85 && hs != "上北" && hs != "下南") safe = "下南";
  if (Math.Abs(zzpos.X - 100) < 1 && zzpos.Y > 115 && hs != "上北" && hs != "下南") safe = "上北";
  if (Math.Abs(zzpos.Y - 100) < 1 && zzpos.X < 85 && hs != "左西" && hs != "右东") safe = "右东";
  if (Math.Abs(zzpos.Y - 100) < 1 && zzpos.X > 115 && hs != "左西" && hs != "右东") safe = "左西";
  if (safe is null) return;
  Place(safe switch {
   "上北" => StaticPlace.safeA,
   "下南" => StaticPlace.safeC,
   "左西" => StaticPlace.safeD,
   "右东" => StaticPlace.safeB
  });
  safe += "安全";
  PostTip(safe);
  Broadcast($"火神转场{safe}");
 }

 private static void p2hs(string xy) {
  var ss = xy.Split(',');
  var id = ss[0];
  if (BridgeFFXIV.GetIdEntity(id).GetValue("bnpcid").ToString() != "8730") return;
  hs = GetDir(ss[1], ss[2]);
  if (hs is "上北" or "下南") Place(StaticPlace.p2hsWEsafe);
  if (hs is "左西" or "右东") Place(StaticPlace.p2hsNSsafe);
  Place(StaticPlace.clear4);
  PostTip("火神" + hs, 50, 50, "main2");
 }


 private static void FsPlace(string pl1, string pl2) {
  Place(pl1 switch {
   "上北" => StaticPlace.p1fsN3,
   "下南" => StaticPlace.p1fsS3,
   "左西" => StaticPlace.p1fsW3,
   "右东" => StaticPlace.p1fsE3
  });
  Place(pl2 switch {
   "上北" => StaticPlace.p1fsN4,
   "下南" => StaticPlace.p1fsS4,
   "左西" => StaticPlace.p1fsW4,
   "右东" => StaticPlace.p1fsE4
  });
 }
 public static void FsPlaceByRule(string pos1, string pos2)
{
    // 3点位优先顺序
    string[] p3Order = { "上北", "右东", "下南", "左西" };
    string[] input = { pos1, pos2 };
    // 按优先级排序
    Array.Sort(input, (a, b) => Array.IndexOf(p3Order, a).CompareTo(Array.IndexOf(p3Order, b)));
    string p3 = input[0];
    string p4 = input[1];
    // 3点位
    switch (p3)
    {
        case "上北": Place(StaticPlace.p1fsN3); break;
        case "右东": Place(StaticPlace.p1fsE3); break;
        case "下南": Place(StaticPlace.p1fsS3); break;
        case "左西": Place(StaticPlace.p1fsW3); break;
    }
    // 4点位
    switch (p4)
    {
        case "上北": Place(StaticPlace.p1fsN4); break;
        case "右东": Place(StaticPlace.p1fsE4); break;
        case "下南": Place(StaticPlace.p1fsS4); break;
        case "左西": Place(StaticPlace.p1fsW4); break;
    }
}


 private static string GetDir(Vector2 v) {
  return GetDir(v.X, v.Y);
 }

 private static string GetDir(string x, string y) {
  return GetDir(float.Parse(x), float.Parse(y));
 }

 private static string GetDir(float x, float y) {
  return x switch {
   > 110 when y > 110 => "右下东南",
   > 110 when y < 90 => "右上东北",
   < 90 when y < 90 => "左上西北",
   < 90 when y > 110 => "左下西南",
   > 115 => "右东",
   < 85 => "左西",
   _ => y switch {
    < 85 => "上北",
    > 115 => "下南",
    _ => ""
   }
  };
 }

 private static List<Vector2> GetXYFromBnpcid(string arg, int reqHP = -1) {
  return arg.Any(c => c is (< '0' or > '9') and (< 'A' or > 'F') and (< 'a' or > 'f'))
   ? null
   : (from en in BridgeFFXIV.GetAllEntities().Where(en => en.GetValue("bnpcid").ToString() == arg)
   where reqHP == -1 || int.Parse(en.GetValue("currenthp").ToString()) == reqHP
   select new Vector2(float.Parse(en.GetValue("x").ToString()), float.Parse(en.GetValue("y").ToString()))).ToList();
 }

 private static Vector2 GetXY(string id_xy) {
  if (id_xy.Contains(",")) {
   var ss = id_xy.Split(',');
   return new Vector2(float.Parse(ss[0]), float.Parse(ss[1]));
  }
  if (id_xy.Any(c => c is (< '0' or > '9') and (< 'A' or > 'F') and (< 'a' or > 'f'))) return new Vector2();
  var e = BridgeFFXIV.GetIdEntity(id_xy);
  return new Vector2(float.Parse(e.GetValue("x").ToString()), float.Parse(e.GetValue("y").ToString()));
 }

 private static void ThreeBucket(string args) {
  foreach (var v in Players) {
   if (args != v.name) continue;
   lock (Threebuckets) {
    switch (Threebuckets.Count) {
     case 0:
      Threebuckets.Add(v);
      break;
     case 1:
      if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
      else Threebuckets.Add(v);
      break;
     case 2:
      if (Threebuckets[0].storder > v.storder) Threebuckets.Insert(0, v);
      else if (Threebuckets[1].storder > v.storder) Threebuckets.Insert(1, v);
      else Threebuckets.Add(v);
      if (Uthreebucket) {
       plug.InvokeNamedCallback("command", $"/mk attack1 <{Threebuckets[0].partyorder}>");
       plug.InvokeNamedCallback("command", $"/mk attack2 <{Threebuckets[1].partyorder}>");
       plug.InvokeNamedCallback("command", $"/mk attack3 <{Threebuckets[2].partyorder}>");
       Task.Run(async delegate {
        await Task.Delay(9000);
        plug.InvokeNamedCallback("command", "/mk attack1 <attack1>");
        plug.InvokeNamedCallback("command", "/mk attack2 <attack2>");
        plug.InvokeNamedCallback("command", "/mk attack3 <attack3>");
       });
      }
      else {
       plug.InvokeNamedCallback("command", $"/e attack1 <{Threebuckets[0].partyorder}>");
       plug.InvokeNamedCallback("command", $"/e attack2 <{Threebuckets[1].partyorder}>");
       plug.InvokeNamedCallback("command", $"/e attack3 <{Threebuckets[2].partyorder}>");
      }
      Threebuckets.Clear();
      break;
    }
   }
   break;
  }
 }

 private static void InitParams() {
  Threebuckets.Clear();
  P43PlayerName.Clear();
  Zhuzis.Clear();
  Zhuzi2 = new Vector2();
  hs = "";
  ttsafe = "";
  hsadd = "";
  ciyu = new Ciyu();
  p43dhCount = 0;
  p43dhDist = new List<DHP43>();
  Ubroadcast = GetPScale("Ubroadcast") == "1";
  Uthreebucket = GetPScale("Uthreebucket") == "1";
  Uauto = GetPScale("Uauto") == "1";
  Umarklocal = GetPScale("Umarklocal") == "1";
  Umark = GetPScale("Umark") == "1";
  
  
 }

 private static void GetPartyOrderInit(string args) {
  var ss = args.Split(',');
  var find = false;
  for (var i = 0; i < 8; i++) {
   var job = JobOrder[i];
   int num;
   if (MyJob == job) {
    num = 1;
    find = true;
   }
   else num = i + (find ? 1 : 2);
   var name = ss[num];
   foreach (var ser in Server) {
    if (!name.EndsWith(ser)) continue;
    name = name.Substring(0, name.Length - ser.Length);
    break;
   }
   Players[i] = new Player(job, num, name);
  }
  InitParams();
  var sb = new StringBuilder("小队初始化完成。职业").Append(MyJob).Append("。");
  if (Ubroadcast) sb.Append("启用团队播报。");
  if (Uthreebucket) sb.Append("启用三连桶点名。");
  if (Uauto) sb.Append("启用FA坐标");
  if (Umark&&Umarklocal) sb.Append("启用本地标记。");
  if (Umark&&!Umarklocal) sb.Append("启用小队可见标记。");
  if (Utarget) sb.Append("启用自动切换目标。");
  
  var sbs = sb.ToString();
  Log(sbs);
  PostTTS(sbs);
 }

 private static void Log(string message) {
  plug.InvokeNamedCallback("command", $"/e {message}");
 }

 private record DHP43(string name, float dist) {
  internal readonly string name = name;
  internal float dist = dist;
 }

 private record P41Info {
  internal readonly Vector2 after;
  internal readonly bool canES;
  internal readonly string desc, first;

  internal P41Info(string first, string desc, Vector2 after, bool canES) {
   this.first = first;
   this.canES = canES;
   this.desc = desc;
   if (canES) this.desc += "(提前安全)";
   this.after = after;
  }

  public override string ToString() {
   return desc;
  }
 }

 private record Ciyu {
  internal readonly List<string> dmg = new();
  internal bool cs2;
  internal string zid;
 }

 private record Zhuzi {
  internal readonly List<string> dmg = new();//伤害
  internal readonly int pos;
  internal readonly string zid;
  internal bool cs2;

  internal Zhuzi(string zid, int pos) {
   this.zid = zid;
   this.pos = pos;
  }
 }

 private static class StaticPlace {
  internal const string initPlace = "A:100,82;B:118,100;C:100,118;D:82,100;3:93,93;4:107,107;1:100,100",
   clear2 = "2:clear",
   clear3 = "3:clear",
   clear4 = "4:clear",
   safeA = "1:100,84;2:98,82;3:102,82;4:100,92.5",
   safeB = "1:116,100;2:118,98;3:118,102;4:107.5,100",
   safeC = "1:100,116;2:102,118;3:98,118;4:100,107.5",
   safeD = "1:84,100;2:82,102;3:82,98;4:92.5,100",
   p1fsN3 = "3:100,90",
   p1fsN4 = "4:100,90",
   p1fsS3 = "3:100,110",
   p1fsS4 = "4:100,110",
   p1fsW3 = "3:90,100",
   p1fsW4 = "4:90,100",
   p1fsE3 = "3:110,100",
   p1fsE4 = "4:110,100",
   p2hsWEsafe = "2:90,100;3:110,100",
   p2hsNSsafe = "2:100,90;3:100,110",
   p41place2 = "2:100,111",
   p42place2 = "2:88,88",
   p42place4d3 = "4:100,110",
   p42place2rf = "2:100,112",
   startp43 = "A:100,82;B:94.5,83;1:89.5,85.5;2:85.5,89.5;3:83,94.5;4:82,100",
   p5jzhA = "B:101,81;C:101,83;D:99,81;4:99,83",
   p5jzh2 = "B:89,87;C:89,89;D:87,87;4:87,89",
   p5jzh3 = "B:94,92;C:94,94;D:92,92;4:92,94";
  internal static readonly string[,] ygjdPlace4 = {
   { "4:102.1,108.6", "4:97.9,108.6" },
   { "4:91.4,102.1", "4:91.4,97.9" },
   { "4:97.9,91.4", "4:102.1,91.4" },
   { "4:108.6,97.9", "4:108.6,102.1" }
  };
 }


 private readonly record struct Player {
  internal readonly string job, name;
  internal readonly int partyorder, storder;

  internal Player(string job, int partyorder, string name) {
   this.job = job;
   if (job == MyJob) MyName = name;
   this.partyorder = partyorder;
   var o = 0;
   for (var i = 0; i < TBJobOrder.Length; i++) {
    if (job != TBJobOrder[i]) continue;
    o = i;
    break;
   }
   storder = o;
   this.name = name;
  }

  public override string ToString() {
   return $"[{job}({partyorder})]:{name}";
  }
 }
}