using System.Collections.Generic;
using System;
using Microsoft.VisualBasic;
using System.Diagnostics;

// SimpleParser class
//
// Author: rkagerer
// Date: June 27, 2009
// Version: 1.01
//
// DESCRIPTION:
//
//   An extremely simple class for extracting meaningful data from a large block of text,
//   such as a web page.  You can use it to search for strings that indicate meaningful
//   data is nearby, and then parse out information by telling it what to look for on
//   the left and right sides of the text you want to extract.
//
// EXAMPLE:
//
//   The fastest way to get started with the SimpleParser is to see it in action.  Here's
//   an example:
//
//     string html;
//     string result;
//     html = "<P>Here's a weather report:</P>"
//          + "<TABLE>"
//          + "  <TH><TD>Day</TD>      <TD>Conditions</TD></TH>"
//          + "  <TR><TD>Friday</TD>   <TD>Sunny</TD></TR>"
//          + "  <TD><TD>Saturday</TD> <TD>Cloudy</TD></TR>"
//          + "</TABLE>";
//
//     SimpleParser parser = new SimpleParser(html);             
//         parser.SeekNext("Friday");                 // Advance to "Friday"
//         parser.SeekNext("</TD>");                  // Advance to the "</TD>" symbol
//         result = parser.Extract("<TD>", "</TD>");  // Extract "Sunny"
//
//     MessageBox.Show("The weather for Friday is: " + result);
//
// PORTABILITY:
//
//  A version is also available for VB.NET 2.0, which compiles in the Appforge Crossfire
//  compiler.
//

/// <summary>
/// Simple class for extracting meaningful data from a large block of text, such as a web
/// page, by looking for specific symbols and extracting the text between them.
/// </summary>
public class SimpleParser {
    
  // Member variables
  private bool _caseSensitive = false;
  private string _text = "";
  private int _cursor = 0;
    
  /// <summary>
  /// Creates a new SimpleParser.
  /// </summary>
  public SimpleParser() {}
    
  /// <summary>
  /// Creates a new SimpleParser.
  /// </summary>
  /// <param name="text">The string to be parsed.</param>
  /// <exception cref="ArgumentNullException"/>
  public SimpleParser(string text) {
    if (text == null) throw new ArgumentNullException("text");
    _text = text;
  }
    
  /// <summary>
  /// Determines whether parsing is sensitive to capitalization differences.
  /// </summary>
  public bool CaseSensitive {
    get { return _caseSensitive; }
    set { _caseSensitive = value; }
  }
    
  /// <summary>
  /// The large block of text being parsed.
  /// </summary>
  /// <remarks>
  ///   <para>Note that setting this value causes the <see cref="Cursor"/> property
  ///   to be reset to 0.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public string Text {
    get { return _text; }
    set {
      if (value == null) throw new ArgumentNullException();
      _text = value;
      _cursor = 0;
    }
  }
    
  /// <summary>
  /// The location of the cursor.
  /// </summary>
  /// <remarks>
  ///   <para>The cursor works just like a cursor in a text editing program, and its value
  ///   can be from 0 (immediately left of the first character) to <c>Text.Length</c>
  ///   (immediately right of the last character).</para>
  ///   <para>Most <c>SimpleParser</c>operations work starting from the cursor.</para>
  /// </remarks>
  /// <exception cref="ArgumentOutOfRangeException">
  ///   Thrown when <paramref name="value"/> is less than zero or greater than the
  ///   length of <see cref="Text"/>.
  /// </exception>
  public int Cursor {
    get { return _cursor; }
    set {
      if (value < 0 || value > Text.Length) {
        throw new ArgumentOutOfRangeException("value",
          "Cursor must be greater than zero, and less than or equal to text length."); 
      }
      _cursor = value;
    }
  }
  
  /// <summary>
  /// Returns <c>True</c> if the cursor is at the left edge of the text.
  /// </summary>
  protected bool CursorIsAtLeftEdge {
    get { return _cursor == 0; }
  }
    
  /// <summary>
  /// Returns <c>True</c> if the cursor is at the right edge of the text.
  /// </summary>
  protected bool CursorIsAtRightEdge {
    get { return _cursor == _text.Length; }
  }

  /// <summary>
  /// Returns the <see cref="System.StringComparison"/> equivalent for
  /// the <see cref="CaseSensitive"/> property.
  /// </summary>
  protected StringComparison CompareMethod {
    get {
      return CaseSensitive ? StringComparison.InvariantCulture :
        StringComparison.InvariantCultureIgnoreCase;
    }
  }
    
  /// <summary>
  /// Places the cursor immediately after the next occurence of the given string.
  /// </summary>
  /// <param name="Symbol">The symbol to look for.</param>
  /// <exception cref="SymbolNotFoundException">
  ///   This exception is thrown when <paramref name="symbol"/> is not found or if the
  ///   cursor is beyond the end of the text.
  /// </exception>
  /// <exception cref="ArgumentNullException"/>
  public void SeekNext(string symbol) {
    SeekNext_helper(symbol, true);
  }
    
  /// <summary>
  /// Attempts to place the cursor immediately after the next occurence of the
  /// given string, returning <c>True</c> if successful.
  /// </summary>
  /// <param name="symbol">The symbol to look for.</param>
  /// <returns>Returns <c>True</c> if the symbol was found, otherwise 
  ///   <c>False</c>.</returns>
  /// <remarks>
  ///   <para>If the symbol is not found, then the cursor is not moved.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public bool TrySeekNext(string symbol) {
    return SeekNext_helper(symbol, false);
  }
    
  // Helper for SeekNext and TrySeekNext
  protected bool SeekNext_helper(string symbol, bool errOnNotFound) {
    
    if (symbol == null) throw new ArgumentNullException("symbol");

    if (CursorIsAtRightEdge) {
      if (errOnNotFound) {
        throw new SymbolNotFoundException(symbol, _cursor, false,
          "Cursor is at end of text.");
      }
      return false;
    }
    
    if (symbol == "") return true;
    
    // Look for the symbol
    int symbPos = _text.IndexOf(symbol, _cursor, CompareMethod);
    
    // Check if found
    if (symbPos == -1) {

      if (errOnNotFound) {
        throw new SymbolNotFoundException(symbol, Cursor, true);
      }
      return false;

    } else {

      // Found the symbol.  Place cursor to the right of it.
      _cursor = symbPos + symbol.Length;
      return true;

    }
  }
    
  /// <summary>
  /// Places the cursor immediately before the previous occurence of the given string.
  /// </summary>
  /// <param name="Symbol">The symbol to look for.</param>
  /// <exception cref="SymbolNotFoundException">
  ///   This exception is thrown when <paramref name="symbol"/> is not found.
  /// </exception>
  /// <exception cref="ArgumentNullException"/>
  public void SeekPrev(string symbol) {
    SeekPrev_helper(symbol, true);
  }
    
  /// <summary>
  /// Attempts to place the cursor immediately before the next occurence of the
  /// given string, returning <c>True</c> if successful.
  /// </summary>
  /// <param name="symbol">The symbol to look for.</param>
  /// <returns>Returns <c>True</c> if the symbol was found, otherwise 
  ///   <c>>False</c>.</returns>
  /// <remarks>
  ///   <para>If the symbol is not found, then the cursor is not moved.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public bool TrySeekPrev(string symbol) {
    return SeekPrev_helper(symbol, false);
  }
    
  // Helper for SeekPrev and TrySeekPrev
  public bool SeekPrev_helper(string symbol, bool errOnNotFound) {

    if (symbol == null) throw new ArgumentNullException("symbol");

    if (CursorIsAtLeftEdge) {
      if (errOnNotFound) {
        throw new SymbolNotFoundException(symbol, _cursor, false,
          "Cursor is at beginning of text.");
      }
      return false;
    }
      
    if (symbol == "") return true;

    // Look for the symbol.  Note (_cursor - 1) is specified since we want
    // to search starting from the character to the left of the cursor.
    int symbPos = _text.LastIndexOf(symbol, _cursor - 1, CompareMethod);
      
    // Check if found
    if (symbPos == -1) {
      if (errOnNotFound) {
        throw new SymbolNotFoundException(symbol, Cursor, false);
      }
      return false;
    } else {
      // Found the symbol.  Place cursor to the left of it.
      _cursor = symbPos;
      return true;
    }
  }
    
  /// <summary>
  /// Extracts the next piece of text occurring between the given symbols.
  /// </summary>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <returns>Returns the extracted text.</returns>
  /// <remarks>
  ///   <para>This function searches forward from the cursor, until it finds an occurence of
  ///   both <paramref name="before"/> and <paramref name="after"/> (in that order). It returns
  ///   the text between them, and advances the cursor to the right of <paramref name="after"/>.
  ///   </para><para>If <paramref name="before"/> is a zero-length string, then extraction begins
  ///   from the cursor.  If <paramref name="after"/> is a zero-length string, then all text to
  ///   the right of <paramref name="before"/> is extracted.</para>
  ///   <para>If either <paramref name="before"/> or <paramref name="after"/> aren't found,
  ///   then the cursor is not moved.</para>
  /// </remarks>
  /// <exception cref="SymbolNotFoundException">
  ///   Thrown when either <paramref name="before"/> or <paramref name="after"/> is not found.
  /// </exception>
  /// <exception cref="ArgumentNullException"/>
  public string Extract(string before, string after) {
    string result = "";
    Extract_helper(before, after, true, true, ref result);
    return result;
  }
    
  /// <summary>
  /// Attempts to extract the next piece of text occurring between the given symbols.
  /// </summary>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <returns>
  ///   <para>Returns the extracted text, or a zero-length string if either
  ///   <paramref name="before"/> or <paramref name="after"/> could not be found.</para>
  /// </returns>
  /// <remarks>
  ///   <para>This function is identical to the <see cref="Extract"/> method, but doesn't
  ///   generate exceptions when the given symbols can't be found.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public string TryExtract(string before, string after) {
    string result = "";
    Extract_helper(before, after, false, true, ref result);
    return result;
  }
    
  /// <summary>
  /// Returns <c>True</c> if it is possible to perform an <see cref="Extract"/> operation
  /// on the given parameters.
  /// </summary>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <remarks>
  ///   <para>This function may be called before an <see cref="Extract"/> or
  ///   <see cref="TryExtract"/> method to determine if the operation will succeeed.</para>
  ///   <para>It does not move the cursor, and will not throw an exception if the
  ///   given symbols couldn't be found.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public bool CanExtract(string before, string after) {
    string result = "";
    return Extract_helper(before, after, false, false, ref result);
  }
    
  /// <summary>
  /// Attempts to extract the next piece of text occurring between the given symbols,
  /// without moving the cursor.
  /// </summary>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <returns>
  ///   <para>Returns the extracted text, or a zero-length string if either
  ///   <paramref name="before"/> or <paramref name="after"/> could not be found.</para>
  /// </returns>
  /// <remarks>
  ///   This method is identical to <see cref="Extract"/>, except that it won't throw
  ///   an exception if the given symbols could not be found, and it won't move the
  ///   cursor.
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public string Peek(string before, string after) {
    string result = "";
    Extract_helper(before, after, false, false, ref result);
    return result;
  }
    
  // Helper that implements the Extract-related functions.
  // Places the extracted text in the byref result variable, and returns true on
  // success.
  private bool Extract_helper(string before, string after,
    bool errOnNotFound, bool moveCursor, ref string result) {

    if (before == null) throw new ArgumentNullException("before");
    if (after == null) throw new ArgumentNullException("after");

    int start = _cursor;
    int s = 0;
    int e = 0;

    // Look for "before" text, starting at cursor
    s = _text.IndexOf(before, start, CompareMethod);
      
    if (s >= 0) {
      
      // Skip to first character after "before" text
      s += before.Length;

      // Look for "after" text
      if (after == "") {
        e = _text.Length; // extract to end
      } else {
        e = _text.IndexOf(after, s, CompareMethod);
      }
      
      if (e >= 0) {
        // Found "after"
        Debug.Assert(e >= s);                           // sanity check (don't extract negative length)
        Debug.Assert(e + after.Length <= _text.Length); // sanity check (don't move cursor past length)
        if (moveCursor) _cursor = e + after.Length;
        result = _text.Substring(s, e - s);
        return true;
      } else {
        // Found "before" but not "after"
        if (errOnNotFound) {
          throw new SymbolNotFoundException(after, s, true);
        }
      }

    } else {

      // "before" text not found
      if (errOnNotFound) {
        throw new SymbolNotFoundException(before, start, true);
      }

    }

    return false; // not found

  }
    
  /// <summary>
  /// Returns the starting character position of the next occurence of the given symbol, or -1
  /// if it can't be found.
  /// </summary>
  /// <exception cref="ArgumentNullException"/>
  public int IndexOfNext(string symbol) {
    if (symbol == null) throw new ArgumentNullException("symbol");
    return _text.IndexOf(symbol, CompareMethod);
  }
    
  /// <summary>
  /// Looks for an instance of the given string, returning true if it can be found before hitting
  /// any of a set of terminating symbols.  Useful for determining if there are more rows in a table.
  /// </summary>
  /// <param name="symbol">The string being sought.</param>
  /// <param name="terminators">A list of strings that will stop the search and return false if
  ///   found before the next occurence of <paramref name="symbol"/></param>
  /// <returns>Returns <c>True</c> if <paramref name="symbol"/> can be found before any of the
  ///   <paramref name="terminators"/> occur.</returns>
  /// <remarks>
  ///   <para>The following code will return <c>True</c> if there are more rows left in a table:</para>
  ///   <para><code>OccursBefore("&lt;TR&gt;", " &lt;/TABLE&gt;")</code>
  ///   </para>
  ///   <para>This function does not move the cursor.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public bool OccursBefore(string symbol, params string[] terminators) {
    List<string> l = new List<string>();
    l.AddRange(terminators);
    return OccursBefore(symbol, l);
  }
    
  /// <summary>
  /// Looks for an instance of the given string, returning true if it can be found before hitting
  /// any of a set of terminating symbols.  Useful for determining if there are more rows in a table.
  /// </summary>
  /// <param name="symbol">The string being sought.</param>
  /// <param name="terminators">A list of strings that will stop the search and return false if
  ///   found before the next occurence of <paramref name="symbol"/></param>
  /// <returns>Returns <c>True</c> if <paramref name="symbol"/> can be found before any of the
  ///   <paramref name="terminators"/> occur.</returns>
  /// <remarks>
  ///   <para>The following code will return <c>True</c> if there are more rows left in a table:</para>
  ///   <para><code>OccursBefore("&lt;TR&gt;", " &lt;/TABLE&gt;")</code>
  ///   </para>
  ///   <para>This function does not move the cursor.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public bool OccursBefore(string symbol, List<string> terminators) {
    if (symbol == null) throw new ArgumentNullException("symbol");
    if (terminators == null) throw new ArgumentNullException("terminators");
    // Find next occurance of symbol
    int symbPos = IndexOfNext(symbol);
    if (symbPos == -1) return false;  // didn't find symbol   
    foreach (string s in terminators) {
      if (IndexOfNext(s) < symbPos) {
        return false;
      }
    }
    return true; // this point reached means success
  }
 
  /// <summary>
  /// Returns the text adjacent to the cursor, to aid in debugging.
  /// </summary>
  /// <returns>Returns the 20 characters on each side of the cursor, with a "»»" symbol marking
  /// the cursor location.</returns>
  public string DbgPeekCursor() {
    return DbgPeekCursor(20);
  }

  /// <summary>
  /// Returns the text adjacent to the cursor, to aid in debugging.
  /// </summary>
  /// <param name="charsToEachSide">Number of characters to return from each side of cursor.</param>
  /// <returns>Returns the text immediately preceeding and following the cursor, with a "»»" symbol marking
  /// the cursor location.</returns>
  public string DbgPeekCursor(int charsToEachSide) {
      
    const string CURSOR_MARKER = "»»"; // if this is changed, don't forget to update the XML above
    
    // Calculate start and end positions, and ensure they don't exceed text bounds
    int left, right;
    string textLeft = "", textRight = "";
    left = _cursor - charsToEachSide;
    right = _cursor + charsToEachSide;
    if (left < 0) left = 0; 
    if (right > _text.Length - 1) right = _text.Length - 1; 
    
    // Extract the text to the left
    if (_cursor > 0) {
      textLeft = _text.Substring(left, _cursor - left);
    }
    
    // Extract the text to the right
    if (_cursor < _text.Length) {
      textRight = _text.Substring(_cursor, right - _cursor + 1);
    }
        
    return textLeft + CURSOR_MARKER + textRight;

  }
    
  /// <summary>
  /// Performs a <see cref="SeekNext"/> operation followed by an <see cref="Extract"/>,
  /// as one atomic operation.
  /// </summary>
  /// <param name="advanceTo">Symbol to seek forward to.</param>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <exception cref="SymbolNotFoundException">
  ///   Thrown when any of <paramref name="advanceTo"/>, <paramref name="before"/> or
  ///   <paramref name="after"/> are not found.
  /// </exception>
  /// <remarks>
  ///   <para>The cursor is moved only if both operations succeed.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public string SeekAndExtract(string advanceTo, string before, string after) {

    if (advanceTo == null) throw new ArgumentNullException("advanceTo");
    if (before == null) throw new ArgumentNullException("before");
    if (after == null) throw new ArgumentNullException("after");

    // Save the cursor position
    int orgCursor = _cursor;

    try {

      // Perform the seek
      this.SeekNext(advanceTo);

      // Perform the extract
      return this.Extract(before, after);

    } catch (Exception ex) {

      // If either operation raised an exception, restore the cursor then propogate
      // the exception.
      _cursor = orgCursor;
      throw ex;

    }
  }
    
  /// <summary>
  /// Performs a <see cref="TrySeekNext"/> followed by a <see cref="TryExtract"/>,
  /// as one atomic operation.
  /// </summary>
  /// <param name="advanceTo">Symbol to seek forward to.</param>
  /// <param name="before">String to the left of the text to extract.</param>
  /// <param name="after">String to the right of the text to extract.</param>
  /// <exception cref="SymbolNotFoundException">
  ///   Thrown when any of <paramref name="advanceTo"/>, <paramref name="before"/> or
  ///   <paramref name="after"/> are not found.
  /// </exception>
  /// <remarks>
  ///   <para>The cursor is moved only if both operations succeed.</para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"/>
  public string TrySeekAndExtract(string advanceTo, string before, string after) {

    if (advanceTo == null) throw new ArgumentNullException("advanceTo");
    if (before == null) throw new ArgumentNullException("before");
    if (after == null) throw new ArgumentNullException("after");

    // Save the cursor position
    int orgCursor = _cursor;
    //bool success = false;
    
    try {
        
      // Attempt the seek.  If it fails, the cursor will not be moved.
      if (!this.TrySeekNext(advanceTo)) return ""; 
      
      // If this point is reached, the cursor has been moved.
      
      // Attempt the extract.
      string result = "";
      if (Extract_helper(before, after, false, true, ref result)) {
        //success = true;
        return result;
      } else {
        // The second operation failed.  Restore the cursor.
        _cursor = orgCursor;
        return "";
      }

    } catch (Exception ex) {
      // If an exception occured in Extract_helper, we want to make sure the cursor
      // property still gets restored.  It is critical that this occurs *before*
      // execution is restored to the invoker of this method.  If we handled doing
      // this in the Finally section of the Try block, then it won't occur until
      // after all exception handlers execute.  If the invoker were to handle the
      // exception and check the Cursor property in that handler, he/she would see
      // the temporarily-modified cursor (and this function would not be atomic!).
      //
      // Thus we must restore the cursor *first*, and *then* pass any exceptions
      // up the chain.
      _cursor = orgCursor;
      throw ex;
    }
  }
    
  /// <summary>
  /// This exception is thrown by <see cref="SimpleParser"/> members if a string being sought
  /// is not found.
  /// </summary>
  /// <remarks>
  ///   <para>You can use the methods prefixed with <c>Try</c> to avoid throwing this
  ///   method.</para>
  /// </remarks>
  public sealed class SymbolNotFoundException : Exception {

    private string _symbol;
    private int _cursor;
    private bool _directionForward;

    private static string directionToString(bool directionIsForward) {
      return directionIsForward ? "forward" : "backward";
    }
    
    /// <summary>
    /// The symbol that was not found.
    /// </summary>
    public string UnfoundSymbol {
      get { return _symbol; }
    }
    
    /// <summary>
    /// The cursor location at which searching began.
    /// </summary>
    public int Cursor {
      get { return _cursor; }
    }
    
    /// <summary>
    /// The direction in which searching took place, 1 for forwards, -1 for backwards.
    /// </summary>
    public int DirectionSought {
      get {
        return _directionForward ? 1 : -1;
      }
    }
    
    internal SymbolNotFoundException(string symbol, int cursor, bool directionIsForward) : 
      base("Could not find symbol " + symbol + " seeking " + 
      directionToString(directionIsForward) + " from position " + cursor + ".") {

      InitHelper(symbol, cursor, directionIsForward);
    }
    
    internal SymbolNotFoundException(string symbol, int cursor, bool directionIsForward,
      string message) : base(message) {
      InitHelper(symbol, cursor, directionIsForward);
    }
    
    internal SymbolNotFoundException(string symbol, int cursor, bool directionIsForward,
      string message, Exception inner) : base(message, inner) {
      InitHelper(symbol, cursor, directionIsForward);
    }
    
    // A helper for the constructor functions
    private void InitHelper(string symbol, int cursor, bool directionIsForward) {
      _symbol = symbol;
      _cursor = cursor;
      _directionForward = directionIsForward;
    }
      
  }
    
#if DEBUG_TESTS
    
  // Unit Test Output method for the function below
  protected static void UTO(string message)
  {
    Debug.Print(message);
  }
    
  // Subjects the SimpleParser class to a series of tests, returning True if it passes.
  public static bool ExecuteUnitTest()
  {
      
    // Note that the collection of tests below is NOT exhaustive, but they do give an
    // indication of the basic functionality of the SimpleParser class.  They should be 
    // helpful in validating after any future code refactoring (e.g. SimpleParser could
    // be rewritten to use exclusively base-1 indexing internally, and this test might
    // assist in verifying the various +1's and -1's were tacked on correctly.
    
    // Some of these tests may be repetitive.. I added things in several times without
    // checking to see if they superceeded parts that were already written.
    // But hey, it's not like we need to worry about having *too many* tests here!
    
    SimpleParser p = default(SimpleParser);
    bool caught = false;
    
    UTO("TESTING THE SIMPLEPARSER CLASS ---");
    
    UTO("Testing constructors...");
    p = new SimpleParser();
    if (p.Text != "" || p.Cursor != 0) return false; 
    p = new SimpleParser("Hello");
    if (p.Text != "Hello" || p.Cursor != 0) return false; 
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing basic functionality...");
    p.Text = "abCdeFghijklmnop123aa1123";
    if (p.Extract("C", "F") != "de") return false; 
    if (p.Cursor != 6) return false; 
    p.SeekNext("i");
    if (p.Cursor != 9) return false; 
    p.SeekPrev("F");
    if (p.Cursor != 5) return false; 
    if (p.Peek("f", "i") != "gh") return false; 
    if (p.Cursor != 5) return false; 
    if (p.SeekAndExtract("123", "1", "3") != "12") return false; 
    if (p.Cursor != 25) return false; 
    if (p.TrySeekPrev("Z")) return false; 
    if (p.Cursor != 25) return false; 
    // Some testing of SeekPrev on the right bound
    p.Cursor = p.Text.Length;
    if (!p.TrySeekPrev("3")) return false; 
    if (p.Cursor != p.Text.Length - 1) return false; 
    p.Text += "Y";
    p.Cursor = p.Text.Length;
    if (!p.TrySeekPrev("Y")) return false; 
    if (p.Cursor != p.Text.Length - 1) return false; 
    // Testing of small string
    p.Text = "A";
    if (!p.TrySeekNext("A")) return false; 
    if (!p.TrySeekPrev("A")) return false;
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing case insensitivity...");
    p.Text = "abc123CdeFghi";
    p.CaseSensitive = true;
    if (p.Extract("C", "F") != "de") return false; 
    p.CaseSensitive = false;
    p.Cursor = 0;
    if (p.Extract("C", "F") != "123Cde") return false;
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing cursor property edges...");
    p.Text = "abcdef";
    p.Cursor = 0;
    p.Cursor = p.Text.Length - 1;
    if (p.TryExtract("a", "b") != "") return false; 
    if (p.Cursor != p.Text.Length - 1) return false;
    p.Cursor += 1; // cursor is allowed to be to the right of the last character
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing cursor right edge exception...");
    p.Text = "abcdef";
    caught = false;
    try {
      p.Cursor = p.Text.Length + 1;
    }
    catch (ArgumentOutOfRangeException) {
      caught = true;
    }
    if (!caught) return false;
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing cursor left edge exception...");
    p.Text = "abcdef";
    caught = false;
    try {
      p.Cursor = -1;
    }
    catch (ArgumentOutOfRangeException) {
      caught = true;
    }
    if (!caught) return false;
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing that SeekNext generates exception if called when cursor is at right edge of text...");
    p.Text = "abc";
    p.Cursor = 3;
    caught = false;
    try {
      p.SeekNext("a");
    }
    catch (SimpleParser.SymbolNotFoundException) {
      caught = true;
    }
    if (!caught) return false; 
    // Now make sure it doesn't throw and exception using the other method
    p.Cursor = 3;
    caught = false;
    try {
      if (p.TrySeekNext("a") != false) return false; 
    }
    catch (SimpleParser.SymbolNotFoundException) {
      caught = true;
    }
    if (caught) return false;
    UTO("Passed" + Environment.NewLine);
    
    UTO("Testing that SeekPrev generates exception if called when cursor is at left edge of text...");
    p.Text = "abc";
    p.Cursor = 0;
    caught = false;
    try {
      p.SeekPrev("a");
    }
    catch (SimpleParser.SymbolNotFoundException) {
      caught = true;
    }
    if (!caught) return false; 
    // Now make sure it doesn't throw and exception using the other method
    p.Cursor = 0;
    caught = false;
    try {
      if (p.TrySeekPrev("a") != false) return false; 
    }
    catch (SimpleParser.SymbolNotFoundException) {
      caught = true;
    }
    if (caught) return false;
    UTO("Passed" + Environment.NewLine);

    UTO("Testing extraction with zero-length arguments...");    
    p.Text = "abc";
    p.Cursor = 0;
    if (p.Extract("", "a") != "") return false;
    p.Cursor = 0;
    if (p.Extract("", "abc") != "") return false;
    p.Cursor = 0;
    if (p.Extract("", "b") != "a") return false;
    p.Cursor = 0;
    if (p.Extract("", "c") != "ab") return false;
    p.Cursor = 0;
    if (p.Extract("", "") != "abc") return false;    
    p.Text = "";
    if (p.Extract("", "") != "") return false;
    p.Cursor = 0;
    if (p.CanExtract("a", "")) return false;
    UTO("Passed" + Environment.NewLine);

    UTO("ALL TESTS PASSED!");
    return true;
  }

#endif

}
