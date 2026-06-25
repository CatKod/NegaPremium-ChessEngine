using System;

namespace NegaPremium
{
    /// <summary>
    /// Bộ ghi nhận và xuất dữ liệu tìm kiếm đồng bộ cho toàn bộ hệ thống Bot.
    /// </summary>
    public static class StatisticsLogger
    {
        public static void LogGUI(
            Position position,
            string botName,
            double elapsedMilliseconds,
            long nodesVisited,
            long movesProcessed = -1,
            long quiescenceNodes = -1,
            long futileSkips = -1,
            long hashProbes = -1,
            long hashCutoffs = -1,
            long hashMoveChecks = -1,
            long hashMoveFound = -1,
            long killerMoveChecks = -1,
            long killerMoveFound = -1)
        {
            if (Restrictions.Output != OutputType.GUI)
                return;

            // 1. Tính toán các thông số cơ sở
            double speed = nodesVisited / Math.Max(elapsedMilliseconds, 1.0);
            double staticEvaluation = Evaluator.Evaluate(position) / 100.0;

            Terminal.WriteLine("-----------------------------------------------------------------------");
            Terminal.WriteLine($"FEN: {position.GetFEN()}");
            Terminal.WriteLine();

            // 2. Thu thập danh sách các chuỗi thông số cột bên phải bàn cờ
            var statsList = new System.Collections.Generic.List<string>
            {
                $"{botName} ({IntPtr.Size * 8}-bit)",
                $"Search time        {elapsedMilliseconds:0} ms",
                $"Search speed       {speed:0} kN/s",
                $"Nodes visited      {nodesVisited}"
            };

            // Kiểm tra điều kiện in nâng cao (Chỉ in nếu tham số được truyền vào khác -1)
            if (movesProcessed != -1)
                statsList.Add($"Moves processed    {(movesProcessed == 0 ? "N/A" : movesProcessed.ToString())}");

            if (quiescenceNodes != -1 && nodesVisited > 0)
                statsList.Add($"Quiescence nodes   {((double)quiescenceNodes / nodesVisited):0.00 %}");

            if (futileSkips != -1 && movesProcessed > 0)
                statsList.Add($"Futility skips     {((double)futileSkips / movesProcessed):0.00 %}");

            if (hashProbes != -1 && hashProbes > 0)
                statsList.Add($"Hash cutoffs       {((double)hashCutoffs / hashProbes):0.00 %}");

            if (hashMoveChecks != -1 && hashMoveChecks > 0)
                statsList.Add($"Hash move found    {((double)hashMoveFound / hashMoveChecks):0.00 %}");

            if (killerMoveChecks != -1 && killerMoveChecks > 0)
                statsList.Add($"Killer move found  {((double)killerMoveFound / killerMoveChecks):0.00 %}");

            statsList.Add($"Static evaluation  {staticEvaluation:+0.00;-0.00}");

            // 3. Đưa mảng chuỗi vào hàm ToString của quân cờ để tự động căn lề song song bên phải
            Terminal.WriteLine(position.ToString(statsList.ToArray()));
            Terminal.WriteLine();
        }
    }
}