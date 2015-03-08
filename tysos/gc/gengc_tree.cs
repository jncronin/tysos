/* This is a C# rewrite of Emin Martinian's Red Black Tree Code in C
 * http://web.mit.edu/~emin/www.old/source_code/red_black_tree/index.html
 * released under the follwoing licence:

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that neither the name of Emin
Martinian nor the names of any contributors are be used to endorse or
promote products derived from this software without specific prior
written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace tysos.gc
{
    unsafe partial class gengc
    {
        /***********************************************************************/
        /*  FUNCTION:  LeftRotate */
        /**/
        /*  INPUTS:  This takes a tree so that it can access the appropriate */
        /*           root and nil pointers, and the node to rotate on. */
        /**/
        /*  OUTPUT:  None */
        /**/
        /*  Modifies Input: tree, x */
        /**/
        /*  EFFECTS:  Rotates as described in _Introduction_To_Algorithms by */
        /*            Cormen, Leiserson, Rivest (Chapter 14).  Basically this */
        /*            makes the parent of x be to the left of x, x the parent of */
        /*            its parent before the rotation and fixes other pointers */
        /*            accordingly. */
        /***********************************************************************/

        void LeftRotate(heap_header* tree, int tree_idx, chunk_header* x)
        {
            chunk_header* y;
            chunk_header* nil = tree->nil;

            /*  I originally wrote this function to use the sentinel for */
            /*  nil to avoid checking for nil.  However this introduces a */
            /*  very subtle bug because sometimes this function modifies */
            /*  the parent pointer of nil.  This can be a problem if a */
            /*  function which calls LeftRotate also uses the nil sentinel */
            /*  and expects the nil sentinel's parent pointer to be unchanged */
            /*  after calling this function.  For example, when RBDeleteFixUP */
            /*  calls LeftRotate it expects the parent pointer of nil to be */
            /*  unchanged. */

            y = x->right;
            x->right = y->left;

            if (y->left != nil) y->left->parent = x; /* used to use sentinel here */
            /* and do an unconditional assignment instead of testing for nil */

            y->parent = x->parent;

            /* instead of checking if x->parent is the root as in the book, we */
            /* count on the root sentinel to implicitly take care of this case */
            if (x == x->parent->left)
            {
                x->parent->left = y;
            }
            else
            {
                x->parent->right = y;
            }
            y->left = x;
            x->parent = y;
        }

        /***********************************************************************/
        /*  FUNCTION:  RighttRotate */
        /**/
        /*  INPUTS:  This takes a tree so that it can access the appropriate */
        /*           root and nil pointers, and the node to rotate on. */
        /**/
        /*  OUTPUT:  None */
        /**/
        /*  Modifies Input?: tree, y */
        /**/
        /*  EFFECTS:  Rotates as described in _Introduction_To_Algorithms by */
        /*            Cormen, Leiserson, Rivest (Chapter 14).  Basically this */
        /*            makes the parent of x be to the left of x, x the parent of */
        /*            its parent before the rotation and fixes other pointers */
        /*            accordingly. */
        /***********************************************************************/

        void RightRotate(heap_header* tree, int tree_idx, chunk_header* y)
        {
            chunk_header* x;
            chunk_header* nil = tree->nil;

            /*  I originally wrote this function to use the sentinel for */
            /*  nil to avoid checking for nil.  However this introduces a */
            /*  very subtle bug because sometimes this function modifies */
            /*  the parent pointer of nil.  This can be a problem if a */
            /*  function which calls LeftRotate also uses the nil sentinel */
            /*  and expects the nil sentinel's parent pointer to be unchanged */
            /*  after calling this function.  For example, when RBDeleteFixUP */
            /*  calls LeftRotate it expects the parent pointer of nil to be */
            /*  unchanged. */

            x = y->left;
            y->left = x->right;

            if (nil != x->right) x->right->parent = y; /*used to use sentinel here */
            /* and do an unconditional assignment instead of testing for nil */

            /* instead of checking if x->parent is the root as in the book, we */
            /* count on the root sentinel to implicitly take care of this case */
            x->parent = y->parent;
            if (y == y->parent->left)
            {
                y->parent->left = x;
            }
            else
            {
                y->parent->right = x;
            }
            x->right = y;
            y->parent = x;
        }

        /***********************************************************************/
        /*  FUNCTION:  TreeInsertHelp  */
        /**/
        /*  INPUTS:  tree is the tree to insert into and z is the node to insert */
        /**/
        /*  OUTPUT:  none */
        /**/
        /*  Modifies Input:  tree, z */
        /**/
        /*  EFFECTS:  Inserts z into the tree as if it were a regular binary tree */
        /*            using the algorithm described in _Introduction_To_Algorithms_ */
        /*            by Cormen et al.  This funciton is only intended to be called */
        /*            by the RBTreeInsert function and not by the user */
        /***********************************************************************/

        void TreeInsertHelp(heap_header* tree, int tree_idx, chunk_header* z)
        {
            /*  This function should only be called by InsertRBTree (see above) */
            chunk_header* x;
            chunk_header* y;
            chunk_header* nil = tree->nil;
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;

#if DEBUG_TREE
            Formatter.Write("TreeInsertHelp: tree: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)tree, "X", Program.arch.DebugOutput);
            Formatter.Write(", tree_idx: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)tree_idx, "X", Program.arch.DebugOutput);
            Formatter.Write(", z: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)z, "X", Program.arch.DebugOutput);
            Formatter.Write(", nil: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)nil, "X", Program.arch.DebugOutput);
            Formatter.Write(", root: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)root, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
#endif

            z->left = z->right = nil;
            y = root;
            x = root->left;
            while (x != nil)
            {
#if DEBUG_TREE
                Formatter.Write("x: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)x, "X", Program.arch.DebugOutput);
                Formatter.Write(", y: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)y, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif

                y = x;
                if (1 == Compare(x, z, tree_idx))
                { /* x.key > z.key */
                    x = x->left;
                }
                else
                { /* x,key <= z.key */
                    x = x->right;
                }
            }
            z->parent = y;
            if ((y == root) ||
                 (1 == Compare(y, z, tree_idx)))
            { /* y.key > z.key */
                y->left = z;
            }
            else
            {
                y->right = z;
            }
        }

        /*  Before calling Insert RBTree the node x should have its key set */

        /***********************************************************************/
        /*  FUNCTION:  RBTreeInsert */
        /**/
        /*  INPUTS:  tree is the red-black tree to insert a node which has a key */
        /*           pointed to by key and info pointed to by info.  */
        /**/
        /*  OUTPUT:  This function returns a pointer to the newly inserted node */
        /*           which is guarunteed to be valid until this node is deleted. */
        /*           What this means is if another data structure stores this */
        /*           pointer then the tree does not need to be searched when this */
        /*           is to be deleted. */
        /**/
        /*  Modifies Input: tree */
        /**/
        /*  EFFECTS:  Creates a node node which contains the appropriate key and */
        /*            info pointers and inserts it into the tree. */
        /***********************************************************************/

        chunk_header* RBTreeInsert(heap_header* tree, int tree_idx, chunk_header* val)
        {
            chunk_header* y;
            chunk_header* x = val;
            chunk_header* newNode;
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;

            TreeInsertHelp(tree, tree_idx, x);
            newNode = x;
            x->red = 1;
            while (x->parent->red == 1)
            { /* use sentinel instead of checking for root */
                if (x->parent == x->parent->parent->left)
                {
                    y = x->parent->parent->right;
                    if (y->red == 1)
                    {
                        x->parent->red = 0;
                        y->red = 0;
                        x->parent->parent->red = 1;
                        x = x->parent->parent;
                    }
                    else
                    {
                        if (x == x->parent->right)
                        {
                            x = x->parent;
                            LeftRotate(tree, tree_idx, x);
                        }
                        x->parent->red = 0;
                        x->parent->parent->red = 1;
                        RightRotate(tree, tree_idx, x->parent->parent);
                    }
                }
                else
                { /* case for x->parent == x->parent->parent->right */
                    y = x->parent->parent->left;
                    if (y->red == 1)
                    {
                        x->parent->red = 0;
                        y->red = 0;
                        x->parent->parent->red = 1;
                        x = x->parent->parent;
                    }
                    else
                    {
                        if (x == x->parent->left)
                        {
                            x = x->parent;
                            RightRotate(tree, tree_idx, x);
                        }
                        x->parent->red = 0;
                        x->parent->parent->red = 1;
                        LeftRotate(tree, tree_idx, x->parent->parent);
                    }
                }
            }
            root->left->red = 0;
            return (newNode);
        }

        /***********************************************************************/
        /*  FUNCTION:  TreeSuccessor  */
        /**/
        /*    INPUTS:  tree is the tree in question, and x is the node we want the */
        /*             the successor of. */
        /**/
        /*    OUTPUT:  This function returns the successor of x or NULL if no */
        /*             successor exists. */
        /**/
        /*    Modifies Input: none */
        /**/
        /*    Note:  uses the algorithm in _Introduction_To_Algorithms_ */
        /***********************************************************************/

        chunk_header* TreeSuccessor(heap_header* tree, int tree_idx, chunk_header* x)
        {
            chunk_header* y;
            chunk_header* nil = tree->nil;
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;

            if (nil != (y = x->right))
            { /* assignment to y is intentional */
                while (y->left != nil)
                { /* returns the minium of the right subtree of x */
                    y = y->left;
                }
                return (y);
            }
            else
            {
                y = x->parent;
                while (x == y->right)
                { /* sentinel used instead of checking for nil */
                    x = y;
                    y = y->parent;
                }
                if (y == root) return (nil);
                return (y);
            }
        }

        /***********************************************************************/
        /*  FUNCTION:  Treepredecessor  */
        /**/
        /*    INPUTS:  tree is the tree in question, and x is the node we want the */
        /*             the predecessor of. */
        /**/
        /*    OUTPUT:  This function returns the predecessor of x or NULL if no */
        /*             predecessor exists. */
        /**/
        /*    Modifies Input: none */
        /**/
        /*    Note:  uses the algorithm in _Introduction_To_Algorithms_ */
        /***********************************************************************/

        chunk_header* TreePredecessor(heap_header* tree, int tree_idx, chunk_header* x)
        {
            chunk_header* y;
            chunk_header* nil = tree->nil;
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;

            if (nil != (y = x->left))
            { /* assignment to y is intentional */
                while (y->right != nil)
                { /* returns the maximum of the left subtree of x */
                    y = y->right;
                }
                return (y);
            }
            else
            {
                y = x->parent;
                while (x == y->left)
                {
                    if (y == root) return (nil);
                    x = y;
                    y = y->parent;
                }
                return (y);
            }
        }

        /***********************************************************************/
        /*  FUNCTION:  RBDeleteFixUp */
        /**/
        /*    INPUTS:  tree is the tree to fix and x is the child of the spliced */
        /*             out node in RBTreeDelete. */
        /**/
        /*    OUTPUT:  none */
        /**/
        /*    EFFECT:  Performs rotations and changes colors to restore red-black */
        /*             properties after a node is deleted */
        /**/
        /*    Modifies Input: tree, x */
        /**/
        /*    The algorithm from this function is from _Introduction_To_Algorithms_ */
        /***********************************************************************/

        void RBDeleteFixUp(heap_header* tree, int tree_idx, chunk_header* x)
        {
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;
            root = root->left;
            chunk_header* w;

            while ((x->red == 0) && (root != x))
            {
                if (x == x->parent->left)
                {
                    w = x->parent->right;
                    if (w->red == 1)
                    {
                        w->red = 0;
                        x->parent->red = 1;
                        LeftRotate(tree, tree_idx, x->parent);
                        w = x->parent->right;
                    }
                    if ((w->right->red == 0) && (w->left->red == 0))
                    {
                        w->red = 1;
                        x = x->parent;
                    }
                    else
                    {
                        if (w->right->red == 0)
                        {
                            w->left->red = 0;
                            w->red = 1;
                            RightRotate(tree, tree_idx, w);
                            w = x->parent->right;
                        }
                        w->red = x->parent->red;
                        x->parent->red = 0;
                        w->right->red = 0;
                        LeftRotate(tree, tree_idx, x->parent);
                        x = root; /* this is to exit while loop */
                    }
                }
                else
                { /* the code below is has left and right switched from above */
                    w = x->parent->left;
                    if (w->red == 1)
                    {
                        w->red = 0;
                        x->parent->red = 1;
                        RightRotate(tree, tree_idx, x->parent);
                        w = x->parent->left;
                    }
                    if ((w->right->red == 0) && (w->left->red == 0))
                    {
                        w->red = 1;
                        x = x->parent;
                    }
                    else
                    {
                        if (w->left->red == 0)
                        {
                            w->right->red = 0;
                            w->red = 1;
                            LeftRotate(tree, tree_idx, w);
                            w = x->parent->left;
                        }
                        w->red = x->parent->red;
                        x->parent->red = 0;
                        w->left->red = 0;
                        RightRotate(tree, tree_idx, x->parent);
                        x = root; /* this is to exit while loop */
                    }
                }
            }
            x->red = 0;
        }

        /***********************************************************************/
        /*  FUNCTION:  RBDelete */
        /**/
        /*    INPUTS:  tree is the tree to delete node z from */
        /**/
        /*    OUTPUT:  none */
        /**/
        /*    EFFECT:  Deletes z from tree and frees the key and info of z */
        /*             using DestoryKey and DestoryInfo.  Then calls */
        /*             RBDeleteFixUp to restore red-black properties */
        /**/
        /*    Modifies Input: tree, z */
        /**/
        /*    The algorithm from this function is from _Introduction_To_Algorithms_ */
        /***********************************************************************/

        void RBDelete(heap_header* tree, int tree_idx, chunk_header* z)
        {
            chunk_header* y;
            chunk_header* x;
            chunk_header* nil = tree->nil;
            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;

            y = ((z->left == nil) || (z->right == nil)) ? z : TreeSuccessor(tree, tree_idx, z);
            x = (y->left == nil) ? y->right : y->left;
            if (root == (x->parent = y->parent))
            { /* assignment of y->p to x->p is intentional */
                root->left = x;
            }
            else
            {
                if (y == y->parent->left)
                {
                    y->parent->left = x;
                }
                else
                {
                    y->parent->right = x;
                }
            }
            if (y != z)
            { /* y should not be nil in this case */
                /* y is the node to splice out and x is its child */

                if (y->red == 0) RBDeleteFixUp(tree, tree_idx, x);

                //tree->DestroyKey(z->key);
                //tree->DestroyInfo(z->info);
                y->left = z->left;
                y->right = z->right;
                y->parent = z->parent;
                y->red = z->red;
                z->left->parent = z->right->parent = y;
                if (z == z->parent->left)
                {
                    z->parent->left = y;
                }
                else
                {
                    z->parent->right = y;
                }
                //free(z); 
            }
            else
            {
                //tree->DestroyKey(y->key);
                //tree->DestroyInfo(y->info);
                if (y->red == 0) RBDeleteFixUp(tree, tree_idx, x);
                //free(y);
            }
        }
    }
}
