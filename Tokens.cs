namespace jfc
{
    /// <summary> The types of tokens in the language </summary>
    public enum TokenType
    {
        PERIOD,
        SEMICOLON,
        COMMA,
        L_PAREN,
        R_PAREN,
        L_BRACKET,
        R_BRACKET,
        AND,
        OR,
        PLUS,
        MINUS,
        TIMES,
        DIVIDE,
        GT,
        GT_EQ,
        LT,
        LT_EQ,
        EQ,
        NEQ,
        PROGRAM_RW,
        IS_RW,
        BEGIN_RW,
        END_RW,
        GLOBAL_RW,
        PROCEDURE_RW,
        VARIABLE_RW,
        INTEGER_RW,
        FLOAT_RW,
        STRING_RW,
        BOOL_RW,
        IF_RW,
        THEN_RW,
        ELSE_RW,
        FOR_RW,
        RETURN_RW,
        NOT_RW,
        TRUE_RW,
        FALSE_RW,
        NUMBER,
        STRING,
        IDENTIFIER
    }

    /// <summary> Represents a token in the program </summary>
    public class Token
    {
        private readonly TokenType _tokenType;

        /// <summary> Class constructor </summary>
        /// <param name="tokenType"> The type of the new token </param>
        /// <param name="tokenMark"> (Optional) additional information about the token </param>
        public Token(TokenType tokenType, object tokenMark = null)
        {
            _tokenType = tokenType;
            TokenMark = tokenMark;
        }

        /// <summary> The type of the token </summary>
        public TokenType TokenType => _tokenType;

        /// <summary> Extra information about the token </summary>
        public object TokenMark { get; set; } = null;
    }
}
