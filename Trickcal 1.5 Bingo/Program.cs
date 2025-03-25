using System.Diagnostics.CodeAnalysis;

namespace Trickcal;

[Serializable]
public struct Vector2(int x, int y) : IEquatable<Vector2>
{
    public int X = x;
    public int Y = y;

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    /// <summary>
    ///     벡터를 다음으로 넘깁니다.
    /// </summary>
    /// <param name="maxSize">최대 사이즈(0-based)</param>
    public Vector2 Add(Vector2 maxSize)
    {
        if ((X = ++X % maxSize.X) == 0) Y = ++Y % maxSize.Y;
        return this;
    }

    /// <summary>
    ///     다음 벡터를 반환합니다. 원본 변수에는 영향이 없습니다.
    /// </summary>
    /// <param name="maxSize">최대 사이즈(0-based)</param>
    /// <returns></returns>
    public Vector2 Added(Vector2 maxSize)
    {
        return new Vector2(X, Y).Add(maxSize);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return base.Equals(obj);
    }

    public bool Equals(Vector2 other)
    {
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);

    public static Vector2 operator *(Vector2 origin, int i)
    {
        return new Vector2(origin.X * i, origin.Y * i);
    }

    public static Vector2 operator /(Vector2 origin, int i)
    {
        return new Vector2(origin.X / i, origin.Y / i);
    }

    public static bool operator ==(Vector2 lhs, Vector2 rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Vector2 lhs, Vector2 rhs)
    {
        return !lhs.Equals(rhs);
    }
}

public static class Program
{
    /// <summary>
    ///     true일 경우, 중복으로 뒤집는 타일 개수를 최소화 해 탐색합니다.
    ///     false일 경우, 뒤집는 칸 개수를 최소화 해 탐색합니다.
    /// </summary>
    private static bool _isOrderByLessDuplication = true;

    /// <summary>
    ///     보드 사이즈
    /// </summary>
    private static int _bSize = 1;

    /// <summary>
    ///     패턴 사이즈
    /// </summary>
    private static Vector2 _pSize = new(1, 1);

    private static readonly List<List<Vector2>> _placement = [];

    private static int _bestCost;
    private static List<Vector2> _bestPlacement = [];

    private static bool[,] _placeDisplay = new bool[1, 1];

    private static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value < max;
    }

    private static void Main(string[] _)
    {
        var stream = new StreamReader(new BufferedStream(Console.OpenStandardInput()));

        _bestCost = int.MaxValue;

        Console.Write("빙고판의 한 변의 크기를 입력하십시오. 값은 0보다 커야합니다. :");
        var line = stream.ReadLine();

        if (line == null || !int.TryParse(line, out _bSize) || _bSize < 1)
        {
            Console.WriteLine("Error : 입력값이 잘못되었습니다. 1 이상의 값을 입력해주십시오.");
            return;
        }

        Console.Write("플립 패턴의 가로 크기를 입력하십시오. 값은 1 ~ 빙고판의 크기 사이여야 합니다.");
        line = stream.ReadLine();
        if (line == null || !int.TryParse(line, out var pX) || !IsInRange(pX, 1, _bSize + 1))
        {
            Console.WriteLine("Error : 입력값이 잘못되었습니다. 범위에 맞는 값을 입력해주십시오.");
            return;
        }

        Console.Write("플립 패턴의 세로 크기를 입력하십시오. 값은 1 ~ 빙고판의 크기 사이여야 합니다.");
        line = stream.ReadLine();
        if (line == null || !int.TryParse(line, out var pY) || !IsInRange(pY, 1, _bSize + 1))
        {
            Console.WriteLine("Error : 입력값이 잘못되었습니다. 범위에 맞는 값을 입력해주십시오.");
            return;
        }

        _pSize = new Vector2(pX, pY);

        var pattern = new bool[_pSize.X, _pSize.Y];

        Console.WriteLine($"플립 패턴을 입력하십시오. 패턴은 {_pSize} * {_pSize} 크기로 입력되어야 합니다.");
        Console.WriteLine("채워진 칸은 '@'를, 빈 칸은 '_'를 사용해 입력하십시오.");

        #region 패턴입력

        for (var y = 0; y < _pSize.Y; y++)
        {
            var str = stream.ReadLine();

            if (str == null || str.Length != _pSize.X)
            {
                Console.WriteLine("Error : 입력값이 잘못되었습니다. 올바른 길이로 입력해주십시오.");
                return;
            }

            for (var x = 0; x < str.Length; x++)
            {
                var c = str[x];
                pattern[x, y] = c switch
                {
                    '@' => true,
                    '_' => false,
                    _ => throw new Exception("Error : input contains illegal char")
                };
            }
        }

        #endregion

        Console.WriteLine("중복으로 뒤집는 타일이 적은 방향으로 검색하시겠습니까? [y/n]");
        Console.Write("n을 누를 경우, 뒤집는 횟수를 최소로 하여 검색합니다. :");

        var key = Console.ReadLine();
        _isOrderByLessDuplication = key != null && (key.Equals("ㅛ") || key.ToLower().Equals("y"));

        var center = _pSize / 2;

        #region 가능한 모든 경우의 수 대입

        for (var y = 0; y < _bSize; y++)
        for (var x = 0; x < _bSize; x++)
        {
            //칠할 수 있는 게 존재하는지 확인
            var any = false;
            //보드의 (x, y)에서 칠할 수 있는 모든 타일 좌표
            List<Vector2> selectable = [];

            for (var r = 0; r < _pSize.Y; r++)
            for (var c = 0; c < _pSize.X; c++)
            {
                if (!pattern[r, c]) continue;

                var by = y + (r - center.Y);
                var bx = x + (c - center.X);

                if (!IsInRange(bx, 0, _bSize) || !IsInRange(by, 0, _bSize)) continue;

                selectable.Add(new Vector2(bx, by));
                any = true;
            }

            if (!any) continue;

            _placement.Add(selectable);
        }

        #endregion

        var coverage = new int[_bSize, _bSize];

        List<Vector2> selected = [];
        var pos = new Vector2(0, 0);

        BackTrack(pos, 0, ref coverage, ref selected);
        Console.WriteLine("\n\n\n계산 완료.");
        Console.WriteLine($"총 {_bestPlacement.Count}개의 칸을 뒤집어 모든 칸을 열 수 있습니다.");
        if (_isOrderByLessDuplication) Console.WriteLine($"중복으로 뒤집는 칸은 총 {_bestCost - _bSize * _bSize}칸입니다.");
        Console.WriteLine(
            $"{(_isOrderByLessDuplication ? "중복을 최소화 하여 타일을 뒤집는" : "최소한의 횟수로 모든 타일을 뒤집는")} 좌표는 다음과 같습니다(@) :");
        for (var y = 0; y < _bSize; y++)
        {
            for (var x = 0; x < _bSize - 1; x++) Console.Write($"{(_placeDisplay[x, y] ? '@' : '_')}|");
            Console.WriteLine($"{(_placeDisplay[_bSize - 1, y] ? '@' : '_')}");
        }

        Console.ReadKey();
    }

    private static void BackTrack(Vector2 pos, int currentCost, ref int[,] coverage, ref List<Vector2> selected)
    {
        var maxSize = Vector2.One * _bSize;

        //구관이 명관인지 확인하기
        if (_isOrderByLessDuplication)
        {
            if (currentCost >= _bestCost)
            {
                Console.WriteLine("중복 영역 개수가 이전 솔루션에 비해 비효율적, 돌아갑니다.");
                return;
            }
        }
        else
        {
            if (selected.Count >= _bestCost)
            {
                Console.WriteLine("플립하는 영역 개수가 이전 솔루션에 비해 비효율적, 돌아갑니다.");
                return;
            }
        }

        //다 칠했는지 확인할 변수
        var all = true;

        for (var y = 0; y < _bSize; y++)
        for (var x = 0; x < _bSize; x++)
        {
            if (coverage[x, y] >= 1) continue;

            all = false;
            break;
        }

        //다 칠했다면 갱신 후 더 갱신 가능한지 확인하기 위해 돌아가기
        //early return을 통해 현재로서는 이 방법이 최선임을 보장
        if (all)
        {
            var record = _isOrderByLessDuplication ? currentCost : selected.Count;
            Console.WriteLine($"새로운 솔루션 확인. {record}의 비용이 소모됩니다.");
            _bestCost = record;
            var s = selected.ToList();
            _bestPlacement = s;

            _placeDisplay = new bool[_bSize, _bSize];

            foreach (var vec in s) _placeDisplay[vec.X, vec.Y] = true;

            return;
        }

        //후보 다 털리면 종료
        //이 경우는 후보 다 털었는데 다 칠하지도 못했을 경우
        if (pos.Added(maxSize) == Vector2.Zero)
        {
            Console.WriteLine("후보 없음, 돌아갑니다.");
            return;
        }

        //다른거부터 확인
        BackTrack(pos.Added(maxSize), currentCost, ref coverage, ref selected);

        //이거 쓰면 어떨지 찍먹해보기
        var cells = _placement[pos.Y * _bSize + pos.X];

        foreach (var index in cells) coverage[index.X, index.Y]++;
        selected.Add(pos);

        //지금 선택 이후 어떻게 될 지 살펴보기
        BackTrack(pos.Added(maxSize), currentCost + cells.Count, ref coverage, ref selected);

        //텄으면 지우고 런
        selected.RemoveAt(selected.Count - 1);

        foreach (var index in cells) coverage[index.X, index.Y]--;
    }
}