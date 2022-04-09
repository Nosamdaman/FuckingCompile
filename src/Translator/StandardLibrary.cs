namespace jfc {
    /// <summary> Class responsible for translating the source code to LLVM IR </summary>
    public partial class Translator {
        private readonly string _lib =
        "; The following is an implementation of the language's standard library in LLVM assembly\n" +
        "\n" +
        "; Import some functions from the standard libraries\n" +
        "declare i32 @printf(i8* nocapture, ...)\n" +
        "declare i32 @getchar()\n" +
        "declare i32 @sscanf(i8* nocapture, i8* nocapture, ...)\n" +
        "declare float @llvm.sqrt.f32(float)\n" +
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
        "define private i1 @putString([128 x i8] %str) {\n" +
        "    %mem = alloca [128 x i8]\n" +
        "    store [128 x i8] %str, [128 x i8]* %mem\n" +
        "    %ptr = getelementptr [128 x i8], [128 x i8]* %mem, i32 0, i32 0\n" +
        "    %retInt = call i32 (i8*, ...) @printf(i8* %ptr)\n" +
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
        "    ; This block is essentially the same as the main block, accept we'll handle the first character a bit differently\n" +
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
        "; Reads a boolean from the command line\n" +
        "define private i1 @getBool() {\n" +
        "    %int = call i32 @getInteger()\n" +
        "    %bool = icmp ne i32 %int, 0\n" +
        "    ret i1 %bool\n" +
        "}\n" +
        "\n" +
        "; Reads a floating-point number from the command-line\n" +
        "define private float @getFloat() {\n" +
        "    ; First we'll allocate everything as needed\n" +
        "    %ptr.strstr = alloca [128 x i8]\n" +
        "    %ptr.str = getelementptr [128 x i8], [128 x i8]* %ptr.strstr, i32 0, i32 0\n" +
        "    %ptr.fmtstr = alloca [3 x i8]\n" +
        "    store [3 x i8] c\"%f\\00\", [3 x i8]* %ptr.fmtstr\n" +
        "    %ptr.fmt = getelementptr [3 x i8], [3 x i8]* %ptr.fmtstr, i32 0, i32 0\n" +
        "    %ptr.errorstr = alloca [33 x i8]\n" +
        "    store [33 x i8] c\"Error reading float, try again: \\00\", [33 x i8]* %ptr.errorstr\n" +
        "    %ptr.error = getelementptr [33 x i8], [33 x i8]* %ptr.errorstr, i32 0, i32 0\n" +
        "    %ptr.float = alloca float\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Main loop\n" +
        "    main:\n" +
        "    %str = call [128 x i8] @getString()\n" +
        "    store [128 x i8] %str, [128 x i8]* %ptr.strstr\n" +
        "    %res = call i32 (i8*, i8*, ...) @sscanf(i8* %ptr.str, i8* %ptr.fmt, float* %ptr.float)\n" +
        "    %cond.fail = icmp slt i32 %res, 1\n" +
        "    br i1 %cond.fail, label %fail, label %success\n" +
        "\n" +
        "    ; Failure to parse the float\n" +
        "    fail:\n" +
        "    call i32 (i8*, ...) @printf(i8* %ptr.error)\n" +
        "    br label %main\n" +
        "\n" +
        "    ; Return the float\n" +
        "    success:\n" +
        "    %float = load float, float* %ptr.float\n" +
        "    ret float %float\n" +
        "}\n" +
        "\n" +
        "; Reads a string from the command-line\n" +
        "define private [128 x i8] @getString() {\n" +
        "    ; First we'll allocate for the variables we need\n" +
        "    %ptr.str = alloca [128 x i8]\n" +
        "    %ptr.count = alloca i32\n" +
        "    %ptr.errorstr = alloca [87 x i8]\n" +
        "    store [87 x i8] c\"Warning: Input must be no more than 127 characters long, your input will be truncated\\0A\\00\", [87 x i8]* %ptr.errorstr\n" +
        "    %ptr.error = getelementptr [87 x i8], [87 x i8]* %ptr.errorstr, i32 0, i32 0\n" +
        "    store i32 0, i32* %ptr.count\n" +
        "    br label %read\n" +
        "\n" +
        "    ; Block for reading characters\n" +
        "    read:\n" +
        "    %1 = call i32 @getchar()\n" +
        "    %char = trunc i32 %1 to i8\n" +
        "    %cond.newline = icmp eq i8 %char, 10\n" +
        "    br i1 %cond.newline, label %cleanup, label %checkError\n" +
        "\n" +
        "    ; Block to fill the rest of the string with null-terminators\n" +
        "    cleanup:\n" +
        "    %2 = load i32, i32* %ptr.count\n" +
        "    %3 = getelementptr [128 x i8], [128 x i8]* %ptr.str, i32 0, i32 %2\n" +
        "    store i8 0, i8* %3\n" +
        "    %4 = add i32 %2, 1\n" +
        "    store i32 %4, i32* %ptr.count\n" +
        "    %5 = icmp sge i32 %4, 128\n" +
        "    br i1 %5, label %end, label %cleanup\n" +
        "\n" +
        "    ; Check if we can add the character or not\n" +
        "    checkError:\n" +
        "    %6 = load i32, i32* %ptr.count\n" +
        "    %7 = icmp slt i32 %6, 127\n" +
        "    br i1 %7, label %insert, label %clearBuffer\n" +
        "\n" +
        "    ; Insert the character then move on to the next one\n" +
        "    insert:\n" +
        "    %8 = load i32, i32* %ptr.count\n" +
        "    %9 = getelementptr [128 x i8], [128 x i8]* %ptr.str, i32 0, i32 %8\n" +
        "    store i8 %char, i8* %9\n" +
        "    %10 = add i32 %8, 1\n" +
        "    store i32 %10, i32* %ptr.count\n" +
        "    br label %read\n" +
        "\n" +
        "    ; Clear the buffer\n" +
        "    clearBuffer:\n" +
        "    %11 = call i32 @getchar()\n" +
        "    %12 = trunc i32 %11 to i8\n" +
        "    %13 = icmp eq i8 %12, 10\n" +
        "    br i1 %13, label %overflow, label %clearBuffer\n" +
        "\n" +
        "    ; We have an overflow\n" +
        "    overflow:\n" +
        "    call i32 (i8*, ...) @printf(i8* %ptr.error)\n" +
        "    br label %cleanup\n" +
        "\n" +
        "    ; Ends execution\n" +
        "    end:\n" +
        "    %str = load [128 x i8], [128 x i8]* %ptr.str\n" +
        "    ret [128 x i8] %str\n" +
        "}\n" +
        "\n" +
        "; Gets the square root of an integer\n" +
        "define private float @sqrt(i32 %int) {\n" +
        "    %num = sitofp i32 %int to float\n" +
        "    %root = call float @llvm.sqrt.f32(float %num)\n" +
        "    ret float %root\n" +
        "}\n" +
        "\n" +
        "; Compares two strings\n" +
        "define private i1 @cmpString([128 x i8] %l, [128 x i8] %r) {\n" +
        "    ; Initialize memory\n" +
        "    %ptr.l = alloca [128 x i8]\n" +
        "    store [128 x i8] %l, [128 x i8]* %ptr.l\n" +
        "    %ptr.r = alloca [128 x i8]\n" +
        "    store [128 x i8] %r, [128 x i8]* %ptr.r\n" +
        "    %ptr.count = alloca i32\n" +
        "    store i32 0, i32* %ptr.count\n" +
        "    br label %loop\n" +
        "\n" +
        "    ; Compare the strings\n" +
        "    loop:\n" +
        "    %1 = load i32, i32* %ptr.count\n" +
        "    %2 = getelementptr [128 x i8], [128 x i8]* %ptr.l, i32 0, i32 %1\n" +
        "    %3 = load i8, i8* %2\n" +
        "    %4 = getelementptr [128 x i8], [128 x i8]* %ptr.r, i32 0, i32 %1\n" +
        "    %5 = load i8, i8* %4\n" +
        "    %cond.eq = icmp eq i8 %3, %5\n" +
        "    br i1 %cond.eq, label %next, label %endFalse\n" +
        "\n" +
        "    ; Go to the next loop iteration or end\n" +
        "    next:\n" +
        "    %6 = add i32 %1, 1\n" +
        "    store i32 %6, i32* %ptr.count\n" +
        "    %cond.end = icmp sge i32 %6, 128\n" +
        "    br i1 %cond.end, label %endTrue, label %loop\n" +
        "\n" +
        "    ; Strings are not equal\n" +
        "    endFalse:\n" +
        "    ret i1 false\n" +
        "\n" +
        "    ; Strings are equal\n" +
        "    endTrue:\n" +
        "    ret i1 true\n" +
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
