using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace SpanSplitBenchmark
{
    class Program
    {
        static void Main(string[] args) => BenchmarkRunner.Run<Benchmark>();
    }

    [MemoryDiagnoser]
    public class Benchmark
	{
        private const char SeparatorChar = ',';
        private static string TestString0 = "art,arst,ar,str,st,rst,ar,st,arst,ars,t,arst,arst,art,arst,ar,str,st,rst,ar,st,arst,ars,t,arst,";
        private static string TestString1 = "arstarstarst,arstarstarst,arstarstarst,arstarstarst,arstarstarst,arstarstarst,arstarstarst,arstarstarst";

        private ReadOnlySpan<char> TestSpan(int index) => index == 0 ? TestString0.AsSpan() : TestString1.AsSpan();

        [Params(0, 1)]
        public int TestCase { get; set; }

        [Benchmark]
        public void Split_CharSeparator_WithSeparate()
        {
            var enumerator = new SpanSplitEnumerator<char>(TestSpan(TestCase), SeparatorChar);
            foreach (var _ in enumerator) { }
        }

        [Benchmark]
        public void Split_CharSeparator_WithMerged()
        {
            var enumerator = new MergedSpanSplitEnumerator<char>(TestSpan(TestCase), SeparatorChar);
            foreach (var _ in enumerator) { }
        }

        [Benchmark]
        public void Split_CharSeparator_WithMerged_Perf()
        {
            var enumerator = new MergedSpanSplitEnumerator_Perf<char>(TestSpan(TestCase), SeparatorChar);
            foreach (var _ in enumerator) { }
        }
    }
	
	public ref struct MergedSpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
		private readonly ReadOnlySpan<T> _separators;
        private readonly T _separator;
		private readonly bool _isSequence;
        private readonly int _separatorLength;
        private int _offset;
        private int _index;

        public MergedSpanSplitEnumerator<T> GetEnumerator() => this;
        public readonly Range Current => new Range(_offset, _offset + _index - _separatorLength);

        internal MergedSpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            _sequence = span;
            _separators = separators;
            _separator = default;
            _isSequence = true;
            (_index, _offset) = (0, 0);
            _separatorLength = _separators.Length;
        }

        internal MergedSpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            _sequence = span;
            _separator = separator;
            _separators = default;
            _isSequence = false;
            (_index, _offset) = (0, 0);
            _separatorLength = 1;
        }

        public bool MoveNext()
        {
            if ((_offset += _index) > _sequence.Length) { return false; }
            var slice = _sequence.Slice(_offset);

            var nextIdx = _isSequence ? slice.IndexOf(_separators) : slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + _separatorLength;
            return true;
        }
    }

    public ref struct MergedSpanSplitEnumerator_Perf<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly ReadOnlySpan<T> _separators;
        private readonly T _separator;
        private readonly bool _isSequence;
        private readonly int _separatorLength;
        private int _offset;
        private int _index;

        public MergedSpanSplitEnumerator_Perf<T> GetEnumerator() => this;
        public readonly Range Current => new Range(_offset, _offset + _index);

        internal MergedSpanSplitEnumerator_Perf(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            _sequence = span;
            _separators = separators;
            _separator = default;
            _isSequence = true;
            _separatorLength = _separators.Length;
            (_index, _offset) = (0, 0 - _separatorLength);
        }

        internal MergedSpanSplitEnumerator_Perf(ReadOnlySpan<T> span, T separator)
        {
            _sequence = span;
            _separator = separator;
            _separators = default;
            _isSequence = false;
            _separatorLength = 1;
            (_index, _offset) = (0, 0 - _separatorLength);
        }

        public bool MoveNext()
        {
            if ((_offset += _index + _separatorLength) > _sequence.Length) { return false; }
            var slice = _sequence.Slice(_offset);

            var nextIdx = _isSequence ? slice.IndexOf(_separators) : slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length);
            return true;
        }
    }

    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly T _separator;
        private int _offset;
        private int _index;

        public SpanSplitEnumerator<T> GetEnumerator() => this;

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            _sequence = span;
            _separator = separator;
            (_index, _offset) = (0, -1);
        }

        public Range Current => new Range(_offset, _offset + _index);

        public bool MoveNext()
        {
            if ((_offset += _index + 1) > _sequence.Length) { return false; }
            var slice = _sequence.Slice(_offset);

            var nextIdx = slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length);
            return true;
        }
    }

    public ref struct SpanSplitSequenceEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly ReadOnlySpan<T> _separator;
        private int _offset;
        private int _index;

        public SpanSplitSequenceEnumerator<T> GetEnumerator() => this;

        internal SpanSplitSequenceEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator)
        {
            _sequence = span;
            _separator = separator;
            _index = 0;
            _offset = 0;
        }

        public Range Current => new Range(_offset, _offset + _index);

        public bool MoveNext()
        {
            if ((_offset += _index) > _sequence.Length) { return false; }
            var slice = _sequence.Slice(_offset);

            var nextIdx = slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + _separator.Length;
            return true;
        }
    }
}
