namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public partial class Translator {
        private readonly string _lib =
        "; The following is an implementation of the language's standard library \n\n" +
        "; Import some functions from the standard libraries\n" +
        "declare i32 @printf(i8* nocapture, ...)\n" +
        "declare i32 @getchar()\n" +
        "\n" +
        "; Global strings\n" +
        "@str.true = private constant [5 x i8] c\"True\\00\"\n" +
        "@str.false = private constant [6 x i8] c\"False\\00\"\n" +
        "@str.int = private constant [3 x i8] c\"%i\\00\"\n" +
        "@str.floatf = private constant [3 x i8] c\"%f\\00\"\n" +
        "@str.floate = private constant [3 x i8] c\"%e\\00\"\n" +
        "\n" +
        "; Writes a boolean\n" +
        "define private i1 @putBool(i1 %bool) {\n" +
        "    ; Determine which string to print\n" +
        "    br i1 %bool, label %true, label %false\n" +
        "\n" +
        "    ; Flag is true\n" +
        "    true:\n" +
        "    %ptr1 = getelementptr [5 x i8], [5 x i8]* @str.true, i32 0, i32 0\n" +
        "    %retInt1 = call i32 (i8*, ...) @printf(i8* %ptr1)\n" +
        "    %ret1 = icmp sge i32 %retInt1, 0\n" +
        "    ret i1 %ret1\n" +
        "\n" +
        "    ; Flag is false\n" +
        "    false:\n" +
        "    %ptr0 = getelementptr [6 x i8], [6 x i8]* @str.false, i32 0, i32 0\n" +
        "    %retInt0 = call i32 (i8*, ...) @printf(i8* %ptr0)\n" +
        "    %ret0 = icmp sge i32 %retInt0, 0\n" +
        "    ret i1 %ret0\n" +
        "}\n" +
        "\n" +
        "; Writes an integer\n" +
        "define private i1 @putInteger(i32 %int) {\n" +
        "    %ptr = getelementptr [3 x i8], [3 x i8]* @str.int, i32 0, i32 0\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, i32 %int)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Writes a floating-point number\n" +
        "define private i1 @putFloat(float %float) {\n" +
        "    ; If the number is 6 orders of magnitude above or below 1, we'll use scientific notation\n" +
        "    %min = fdiv float 1.0, 1.0e+6\n" +
        "    %cond0 = fcmp ole float %float, %min\n" +
        "    %cond1 = fcmp oge float %float, 1.0e+6\n" +
        "    %cond = or i1 %cond0, %cond1\n" +
        "    %ptrf = getelementptr [3 x i8], [3 x i8]* @str.floatf, i32 0, i32 0\n" +
        "    %ptre = getelementptr [3 x i8], [3 x i8]* @str.floate, i32 0, i32 0\n" +
        "    %ptr = select i1 %cond, i8* %ptre, i8* %ptrf\n" +
        "\n" +
        "    ; Print the number\n" +
        "    %double = fpext float %float to double\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, double %double)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Writes a string\n" +
        "define private i1 @putString(i8* %str) {\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %str)\n" +
        "    %ret = icmp sge i32 %retInt, 0\n" +
        "    ret i1 %ret\n" +
        "}\n" +
        "\n" +
        "; Gets an integer\n" +
        "define private i32 @getInteger() {\n" +
        "    ; We'll start by writing some constants\n" +
        "    %errMsgStr = alloca [31 x i8]\n" +
        "    store [31 x i8] c\"Error reading int, try again: \\00\", [31 x i8]* %errMsgStr\n" +
        "    %errMsgPtr = getelementptr [31 x i8], [31 x i8]* %errMsgStr, i32 0, i32 0\n" +
        "\n" +
        "    ; We'll allocate for an integer\n" +
        "    %ptr = alloca i32\n" +
        "    store i32 0, i32* %ptr\n" +
        "\n" +
        "    ; This block is essentially the same as the main block, accept we'll handle the first character abit          differently\n" +
        "    %signPtr = alloca i32\n" +
        "    %int0 = call i32 @getchar()\n" +
        "    %char0 = trunc i32 %int0 to i8\n" +
        "    %condearlynewline = icmp eq i8 %char0, 10\n" +
        "    br i1 %condearlynewline, label %errorend, label %getsign\n" +
        "\n" +
        "    ; Determine the sign of the integer\n" +
        "    getsign:\n" +
        "    %condnegative = icmp eq i8 %char0, 45\n" +
        "    br i1 %condnegative, label %negative, label %positive\n" +
        "\n" +
        "    ; The integer is negative\n" +
        "    negative:\n" +
        "    store i32 -1, i32* %signPtr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; The integer is positive\n" +
        "    positive:\n" +
        "    store i32 1, i32* %signPtr\n" +
        "    %condgt0 = icmp ugt i8 %char0, 47\n" +
        "    %condlt0 = icmp ult i8 %char0, 58\n" +
        "    %condinrange0 = and i1 %condgt0, %condlt0\n" +
        "    br i1 %condinrange0, label %continue0, label %error\n" +
        "\n" +
        "    ; Continue on\n" +
        "    continue0:\n" +
        "    %num0 = call i32 @charToInt(i8 %char0)\n" +
        "    %old0 = load i32, i32* %ptr\n" +
        "    %update0 = mul i32 %old0, 10\n" +
        "    %new0 = add i32 %num0, %update0\n" +
        "    store i32 %new0, i32* %ptr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Now we'll read a character\n" +
        "    main:\n" +
        "    %int = call i32 @getchar()\n" +
        "    %char = trunc i32 %int to i8\n" +
        "    %condnewline = icmp eq i8 %char, 10\n" +
        "    br i1 %condnewline, label %end, label %inrange\n" +
        "\n" +
        "    ; Newline was read, return\n" +
        "    end:\n" +
        "    %val = load i32, i32* %ptr\n" +
        "    %sign = load i32, i32* %signPtr\n" +
        "    %finalval = mul i32 %val, %sign\n" +
        "    ret i32 %finalval\n" +
        "\n" +
        "    ; Now ensure the character is a number\n" +
        "    inrange:\n" +
        "    %condgt = icmp ugt i8 %char, 47\n" +
        "    %condlt = icmp ult i8 %char, 58\n" +
        "    %condinrange = and i1 %condgt, %condlt\n" +
        "    br i1 %condinrange, label %continue, label %error\n" +
        "\n" +
        "    ; Continue on\n" +
        "    continue:\n" +
        "    %num = call i32 @charToInt(i8 %char)\n" +
        "    %old = load i32, i32* %ptr\n" +
        "    %update = mul i32 %old, 10\n" +
        "    %new = add i32 %num, %update\n" +
        "    store i32 %new, i32* %ptr\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Error block for when we read a bad symbol\n" +
        "    error:\n" +
        "    %errint = call i32 @getchar()\n" +
        "    %errchar = trunc i32 %errint to i8\n" +
        "    %conderr = icmp eq i8 %errchar, 10\n" +
        "    br i1 %conderr, label %errorend, label %error\n" +
        "\n" +
        "    ; Print a message and try again\n" +
        "    errorend:\n" +
        "    call i32 (i8*, ...) @printf(i8* %errMsgPtr)\n" +
        "    %res = call i32 @getInteger()\n" +
        "    ret i32 %res\n" +
        "}\n" +
        "\n" +
        "; Converts a char to an int\n" +
        "define private i32 @charToInt(i8 %char) {\n" +
        "    %norm = sub i8 %char, 48\n" +
        "    %int = zext i8 %norm to i32\n" +
        "    ret i32 %int\n" +
        "}\n";
    }
}
