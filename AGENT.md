# Copilot Guidelines 

**1. Think Before Coding**
- State your assumptions explicitly before generating a solution. If you are uncertain, ask rather than guess.
- Present multiple interpretations when ambiguity exists.
- Push back when a simpler approach exists. Stop when confused.

**2. Read Before You Write**
- Before adding or changing code, review existing exports, immediate callers, and shared utilities.
- Do not assume a piece of code is isolated. If unsure why code is structured a certain way, ask.

**3. Simplicity First**
- Write the minimum amount of code that solves the problem. No speculative features.
- No abstractions for single-use code. 
- If you write 200 lines and it could be 50, simplify it.

**4. Surgical Changes & Conventions**
- Touch only what you must. Clean up only your own mess.
- Match the codebase's conventions and existing style exactly, even if you disagree with them. 
- Do not "improve" adjacent code, comments, or formatting that wasn't asked for.

**5. Goal-Driven Execution**
- Transform tasks into verifiable goals.
- Tests must encode WHY behavior matters, not just WHAT it does.
- Do not just follow steps blindly; define success criteria and iterate until verified.

**6. Safety & Governance**
- Always work inside the project's virtual environment located at `.venv`
- Never run `pip install` globally
- Always activate the venv first with: `source .venv/bin/activate`
- When suggesting terminal commands, always include venv activation.
- If installing packages, use: `source .venv/bin/activate && pip install ...`
- Do not suggest installing packages globally or without venv activation.