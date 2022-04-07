; The following is an implementation of the language's standard library in LLVM assembly
target triple = "x86_64-pc-linux-gnu"

; Import some functions from the standard libraries
declare i32 @printf(i8* nocapture, ...)
declare i32 @getchar()

; Global strings
@str.true = private constant [5 x i8] c"True\00"
@str.false = private constant [6 x i8] c"False\00"
@str.int = private constant [3 x i8] c"%i\00"
@str.floatf = private constant [3 x i8] c"%f\00"
@str.floate = private constant [3 x i8] c"%e\00"

; Writes a boolean
define private i1 @putBool(i1 %bool) {
    ; Determine which string to print
    br i1 %bool, label %true, label %false

    ; Flag is true
    true:
    %ptr1 = getelementptr [5 x i8], [5 x i8]* @str.true, i32 0, i32 0
    %retInt1 = call i32 (i8*, ...) @printf(i8* %ptr1)
    %ret1 = icmp sge i32 %retInt1, 0
    ret i1 %ret1

    ; Flag is false
    false:
    %ptr0 = getelementptr [6 x i8], [6 x i8]* @str.false, i32 0, i32 0
    %retInt0 = call i32 (i8*, ...) @printf(i8* %ptr0)
    %ret0 = icmp sge i32 %retInt0, 0
    ret i1 %ret0
}

; Writes an integer
define private i1 @putInteger(i32 %int) {
    %ptr = getelementptr [3 x i8], [3 x i8]* @str.int, i32 0, i32 0
    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, i32 %int)
    %ret = icmp sge i32 %retInt, 0
    ret i1 %ret
}

; Writes a floating-point number
define private i1 @putFloat(float %float) {
    ; If the number is 6 orders of magnitude above or below 1, we'll use scientific notation
    %min = fdiv float 1.0, 1.0e+6
    %cond0 = fcmp ole float %float, %min
    %cond1 = fcmp oge float %float, 1.0e+6
    %cond = or i1 %cond0, %cond1
    %ptrf = getelementptr [3 x i8], [3 x i8]* @str.floatf, i32 0, i32 0
    %ptre = getelementptr [3 x i8], [3 x i8]* @str.floate, i32 0, i32 0
    %ptr = select i1 %cond, i8* %ptre, i8* %ptrf

    ; Print the number
    %double = fpext float %float to double
    %retInt = call i32 (i8*, ...) @printf(i8* %ptr, double %double)
    %ret = icmp sge i32 %retInt, 0
    ret i1 %ret
}

; Writes a string
define private i1 @putString([128 x i8] %str) {
    %mem = alloca [128 x i8]
    store [128 x i8] %str, [128 x i8]* %mem
    %ptr = getelementptr [128 x i8], [128 x i8]* %mem, i32 0, i32 0
    %retInt = call i32 (i8*, ...) @printf(i8* %ptr)
    %ret = icmp sge i32 %retInt, 0
    ret i1 %ret
}

; Gets an integer
define private i32 @getInteger() {
    ; We'll start by writing some constants
    %errMsgStr = alloca [31 x i8]
    store [31 x i8] c"Error reading int, try again: \00", [31 x i8]* %errMsgStr
    %errMsgPtr = getelementptr [31 x i8], [31 x i8]* %errMsgStr, i32 0, i32 0

    ; We'll allocate for an integer
    %ptr = alloca i32
    store i32 0, i32* %ptr

    ; This block is essentially the same as the main block, accept we'll handle the first character a bit differently
    %signPtr = alloca i32
    %int0 = call i32 @getchar()
    %char0 = trunc i32 %int0 to i8
    %condearlynewline = icmp eq i8 %char0, 10
    br i1 %condearlynewline, label %errorend, label %getsign

    ; Determine the sign of the integer
    getsign:
    %condnegative = icmp eq i8 %char0, 45
    br i1 %condnegative, label %negative, label %positive

    ; The integer is negative
    negative:
    store i32 -1, i32* %signPtr
    br label %main

    ; The integer is positive
    positive:
    store i32 1, i32* %signPtr
    %condgt0 = icmp ugt i8 %char0, 47
    %condlt0 = icmp ult i8 %char0, 58
    %condinrange0 = and i1 %condgt0, %condlt0
    br i1 %condinrange0, label %continue0, label %error

    ; Continue on
    continue0:
    %num0 = call i32 @charToInt(i8 %char0)
    %old0 = load i32, i32* %ptr
    %update0 = mul i32 %old0, 10
    %new0 = add i32 %num0, %update0
    store i32 %new0, i32* %ptr
    br label %main

    ; Now we'll read a character
    main:
    %int = call i32 @getchar()
    %char = trunc i32 %int to i8
    %condnewline = icmp eq i8 %char, 10
    br i1 %condnewline, label %end, label %inrange

    ; Newline was read, return
    end:
    %val = load i32, i32* %ptr
    %sign = load i32, i32* %signPtr
    %finalval = mul i32 %val, %sign
    ret i32 %finalval

    ; Now ensure the character is a number
    inrange:
    %condgt = icmp ugt i8 %char, 47
    %condlt = icmp ult i8 %char, 58
    %condinrange = and i1 %condgt, %condlt
    br i1 %condinrange, label %continue, label %error

    ; Continue on
    continue:
    %num = call i32 @charToInt(i8 %char)
    %old = load i32, i32* %ptr
    %update = mul i32 %old, 10
    %new = add i32 %num, %update
    store i32 %new, i32* %ptr
    br label %main

    ; Error block for when we read a bad symbol
    error:
    %errint = call i32 @getchar()
    %errchar = trunc i32 %errint to i8
    %conderr = icmp eq i8 %errchar, 10
    br i1 %conderr, label %errorend, label %error

    ; Print a message and try again
    errorend:
    call i32 (i8*, ...) @printf(i8* %errMsgPtr)
    %res = call i32 @getInteger()
    ret i32 %res
}

; Converts a char to an int
define private i32 @charToInt(i8 %char) {
    %norm = sub i8 %char, 48
    %int = zext i8 %norm to i32
    ret i32 %int
}

define float @makef(i32 %l, i32 %r, i32 %div) {
    %lf = sitofp i32 %l to float
    %rf = sitofp i32 %r to float
    %rdiv = sitofp i32 %div to float
    %dec = fdiv float %rf, %rdiv
    %num = fadd float %lf, %dec
    ret float %num
}

define i32 @main() {
    ; Create some constants
    %str.nl = alloca [2 x i8]
    store [2 x i8] c"\0A\00", [2 x i8]* %str.nl
    %ptr.nl = getelementptr [2 x i8], [2 x i8]* %str.nl, i32 0, i32 0

    ; Print some stuff
    %1 = call i1 @putInteger(i32 -100)
    call i1 @putString(i8* %ptr.nl)
    call i1 @putBool(i1 %1)
    call i1 @putString(i8* %ptr.nl)
    call i1 @putInteger(i32 100)
    call i1 @putString(i8* %ptr.nl)
    call i1 @putInteger(i32 10045)
    call i1 @putString(i8* %ptr.nl)
    %num0 = call float @makef(i32 2, i32 6, i32 100)
    call i1 @putFloat(float %num0)
    call i1 @putString(i8* %ptr.nl)
    %num1 = call float @makef(i32 2000000, i32 6, i32 100)
    call i1 @putFloat(float %num1)
    call i1 @putString(i8* %ptr.nl)
    %num2 = call float @makef(i32 0, i32 6, i32 1000000000)
    call i1 @putFloat(float %num2)
    call i1 @putString(i8* %ptr.nl)

    ; Return successfully
    ret i32 0
}
