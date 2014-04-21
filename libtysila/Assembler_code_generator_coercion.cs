/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/* The coercion mechanism
 * 
 * Following the variable assignment phase, we should have a code fragment (opcode_match)
 * and three variables, code_O1, code_O2, code_R, all of which are hloc_constraint.specific
 * 
 * The coercion mechanism takes the form:
 * 
 * code_O1 ---+                   +--> coerced_O1 ---+
 * (specific) |                   |    (any type)    |
 *            +--> pre_coercion --+                  +--> code fragment --> code_output_R --> post_coercion --> code_R
 *            |    (function)     |                  |                      (any type)        (function)        (specific)
 * code_O2 ---+                   +--> coerced_O2 ---+
 * (specific)                          (any type)
 * 
 * We thus define two functions, pre_coercion and post_coercion
 * 
 * coerced_O1, coerced_O2 and code_output_R are the expected inputs and outputs of the code fragment being tested,
 * although we have to modify code_output_R if it is expected to be the same as O1 or O2, but only after we have actually
 * defined O1 and O2 (i.e. we coerce the result after we do the inputs)
 * 
 * We define actual_O1, actual_O2 and actual_R to be the specific locations used by the code fragment
 * 
 * 
 * Thus pre_coercion takes as input code_O1, code_O2, coerced_O1, coerced_O2 and returns as output actual_O1, actual_O2,
 *  pre_ and post_coercion CodeBlocks and a cost
 *  
 * post_coercion takes as input code_R, code_output_R and returns as output actual_R, pre_ and post_coercion CodeBlocks
 *  and a cost
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    partial class Assembler
    {
    }
}
