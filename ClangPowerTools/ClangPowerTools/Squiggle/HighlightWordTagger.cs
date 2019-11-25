﻿using System;
using System.Collections.Generic;
using System.Linq;
using ClangPowerTools.Error;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace ClangPowerTools.Squiggle
{

  /// <summary>
  /// This tagger will provide tags for every word in the buffer that
  /// matches the word currently under the cursor.
  /// </summary>
  public class HighlightWordTagger : ITagger<HighlightWordTag>
  {
    #region Members


    private readonly TaskErrorModel error;

    private ITextBuffer SourceBuffer { get; set; }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    #endregion

    #region Constructor

    public HighlightWordTagger(ITextBuffer sourceBuffer)
    {
      SourceBuffer = sourceBuffer;
    }

    #endregion

    #region ITagger<HighlightWordTag> Implementation

    /// <summary>
    /// Find every instance of CurrentWord in the given span
    /// </summary>
    /// <param name="spans">A read-only span of text to be searched for instances of CurrentWord</param>
    public IEnumerable<ITagSpan<HighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
      if (TaskErrorViewModel.Errors == null || TaskErrorViewModel.Errors.Count == 0)
      {
        yield break;
      }

      foreach (var error in TaskErrorViewModel.Errors)
      {
        var column = error.Column;
        var highlightLine = error.Line;
        var characterCount = 0;
        var lines = SourceBuffer.CurrentSnapshot.Lines.ToList();

        if (highlightLine > lines.Count)
          continue;

        if (column < 0)
          column = 0;

        if (highlightLine < 0)
          highlightLine = 0;

        var currentLine = SourceBuffer.CurrentSnapshot.GetLineFromLineNumber(highlightLine);
        var text = currentLine.GetText().TrimEnd();

        if (column >= text.Length)
          column = text.Length - 1;

        if (column - 1 >= 0 && column + 1 < text.Length)
        {
          if (text[column - 1] == ' ' && text[column] != ' ' && text[column + 1] == ' ')
          {
            var snapshotSpanForOneElement = new SnapshotSpan(SourceBuffer.CurrentSnapshot, column, 1);
            var squiggleTag = new HighlightWordTag("error", error.Text);
            SquiggleViewModel.Squiggles.Add(squiggleTag);
            yield return new TagSpan<HighlightWordTag>(snapshotSpanForOneElement, squiggleTag);
            continue;
          }
        }

        for (int i = 0; i < highlightLine; i++)
          characterCount += lines[i].GetText().Length;

        if (string.IsNullOrWhiteSpace(text))
          continue;

        if (column >= text.Length)
          continue;

        var start = characterCount + column + highlightLine + highlightLine + 1;
        if (start < 0)
          start = 0;

        if (start >= SourceBuffer.CurrentSnapshot.GetText().Length)
        {
          start = SourceBuffer.CurrentSnapshot.GetText().Length - 1;
        }

        var iterations = 0;
        for (int i = column; i >= 0; --i)
        {
          if (text[i] == ' ' || text[i] == '\n' || text[i] == '\r')
          {
            break;
          }

          ++iterations;
          --start;
        }

        var length = 0;
        for (int i = column - iterations + 1; i < text.Length; ++i)
        {
          if (text[i] == ' ')
            break;

          ++length;
        }

        var startPoint = start;
        var highlightLength = length;
        var snapshotSpan = new SnapshotSpan(SourceBuffer.CurrentSnapshot, startPoint, highlightLength);

        var squiggle = new HighlightWordTag("error", error.Text);
        SquiggleViewModel.Squiggles.Add(squiggle);

        yield return new TagSpan<HighlightWordTag>(snapshotSpan, squiggle);
      }

    }

    #endregion
  }

}
