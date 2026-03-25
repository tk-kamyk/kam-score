using System.Text.RegularExpressions;
using KamSquare.KamScore.Migration.Models;

namespace KamSquare.KamScore.Migration;

public partial class SqlDumpParser
{
    private readonly string _content;

    public SqlDumpParser(string filePath)
    {
        _content = File.ReadAllText(filePath);
    }

    public List<MysqlTournament> ParseTournaments()
    {
        return ParseInsert("tournament", values => new MysqlTournament(
            Long(values[0]),
            Int(values[1]),
            Str(values[2]),
            Str(values[3]),
            Int(values[4]),
            NullLong(values[5])
        ));
    }

    public List<MysqlTeam> ParseTeams()
    {
        return ParseInsert("team", values => new MysqlTeam(
            Long(values[0]),
            Int(values[1]),
            Str(values[2]),
            Long(values[3]),
            NullStr(values[4]),
            NullStr(values[5]),
            NullStr(values[6])
        ));
    }

    public List<MysqlPhase> ParsePhases()
    {
        return ParseInsert("phase", values => new MysqlPhase(
            Long(values[0]),
            Str(values[1]),
            Str(values[2]),
            Long(values[3])
        ));
    }

    public List<MysqlGroup> ParseGroups()
    {
        return ParseInsert("team_group", values => new MysqlGroup(
            Long(values[0]),
            Str(values[1]),
            Long(values[2])
        ));
    }

    public List<MysqlGame> ParseGames()
    {
        return ParseInsert("game", values => new MysqlGame(
            Long(values[0]),
            NullInt(values[1]),
            Bool(values[2]),
            NullStr(values[3]),
            NullInt(values[4]),
            NullLong(values[5]),
            Long(values[6]),
            NullLong(values[7]),
            NullLong(values[8]),
            Long(values[9])
        ));
    }

    public List<MysqlRound> ParseRounds()
    {
        return ParseInsert("round", values => new MysqlRound(
            Long(values[0]),
            Str(values[1]),
            NullStr(values[2]),
            Long(values[3])
        ));
    }

    public List<MysqlPlayoff> ParsePlayoffs()
    {
        return ParseInsert("playoff", values => new MysqlPlayoff(
            Long(values[0]),
            NullStr(values[1]),
            Long(values[2])
        ));
    }

    public List<MysqlCup> ParseCups()
    {
        return ParseInsert("cup", values => new MysqlCup(
            Long(values[0]),
            Str(values[1]),
            NullLong(values[2]),
            NullLong(values[3]),
            Long(values[4]),
            NullLong(values[5]),
            NullLong(values[6])
        ));
    }

    public List<MysqlPlayoffGame> ParsePlayoffGames()
    {
        return ParseInsert("playoff_game", values => new MysqlPlayoffGame(
            Long(values[0]),
            Bool(values[1]),
            NullStr(values[2]),
            NullLong(values[3]),
            Long(values[4]),
            NullLong(values[5]),
            NullLong(values[6]),
            NullLong(values[7])
        ));
    }

    public List<MysqlGameSet> ParseGameSets()
    {
        return ParseInsert("game_set", values => new MysqlGameSet(
            Long(values[0]),
            NullInt(values[1]),
            NullInt(values[2]),
            Long(values[3])
        ));
    }

    public List<MysqlStanding> ParseStandings()
    {
        return ParseInsert("standing", values => new MysqlStanding(
            Long(values[0]),
            Int(values[1]),
            Int(values[2]),
            Int(values[3]),
            Int(values[4]),
            Float(values[5]),
            Long(values[6]),
            Long(values[7])
        ));
    }

    private List<T> ParseInsert<T>(string tableName, Func<List<string>, T> mapper)
    {
        var results = new List<T>();
        var pattern = $@"INSERT INTO `{Regex.Escape(tableName)}` VALUES ";

        foreach (var line in _content.Split('\n'))
        {
            if (!line.StartsWith(pattern)) continue;

            var data = line[pattern.Length..].TrimEnd(';');
            var rows = SplitRows(data);

            foreach (var row in rows)
            {
                var values = SplitValues(row);
                results.Add(mapper(values));
            }
        }

        return results;
    }

    /// <summary>
    /// Splits "(v1,v2),(v3,v4)" into individual row strings "v1,v2" and "v3,v4".
    /// Handles nested parentheses and quoted strings with escaped characters.
    /// </summary>
    private static List<string> SplitRows(string data)
    {
        var rows = new List<string>();
        var depth = 0;
        var start = -1;
        var inString = false;
        var escaped = false;

        for (var i = 0; i < data.Length; i++)
        {
            var c = data[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '\'' && !escaped)
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '(')
            {
                if (depth == 0) start = i + 1;
                depth++;
            }
            else if (c == ')')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    rows.Add(data[start..i]);
                    start = -1;
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Splits a row's comma-separated values, respecting quoted strings.
    /// Returns raw value strings (with quotes for strings, NULL for nulls).
    /// </summary>
    private static List<string> SplitValues(string row)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inString = false;
        var escaped = false;

        for (var i = 0; i < row.Length; i++)
        {
            var c = row[i];

            if (escaped)
            {
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '\'')
            {
                inString = !inString;
                current.Append(c);
                continue;
            }

            if (c == ',' && !inString)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }

    private static long Long(string v) => long.Parse(v);
    private static int Int(string v) => int.Parse(v);
    private static float Float(string v) => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
    private static bool Bool(string v) => v == "1";
    private static long? NullLong(string v) => v == "NULL" ? null : long.Parse(v);
    private static int? NullInt(string v) => v == "NULL" ? null : int.Parse(v);

    private static string Str(string v)
    {
        if (v == "NULL") return string.Empty;
        // Strip surrounding quotes
        if (v.StartsWith('\'') && v.EndsWith('\''))
            v = v[1..^1];
        return v;
    }

    private static string? NullStr(string v)
    {
        if (v == "NULL") return null;
        if (v.StartsWith('\'') && v.EndsWith('\''))
            v = v[1..^1];
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
