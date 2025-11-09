# OAuth Google Implementation - Full Agent Cycle Plan

**Status**: Ready for execution
**Target**: Complete OAuth implementation across all 5 phases
**Timeline**: 3 weeks (parallel execution where possible)

---

## Agent Cycle Strategy

### Overview
This plan specifies which specialized agents will handle each phase, in what order, and how they coordinate.

### Agent Role Matrix

| Phase | Task | Primary Agent | Support Agents | Rationale |
|-------|------|---------------|-----------------|-----------|
| 1 | DB Schema Analysis | `codebase-researcher` | `Explore` | Need to understand current User model structure |
| 1 | DB Schema Implementation | `plan-task-executor` | N/A | Straight EF Core migration code |
| 2 | Backend Architecture | `work-plan-architect` | `codebase-researcher` | Complex multi-step auth flow |
| 2 | AuthService Implementation | `plan-task-executor` | `code-principles-reviewer` | Core business logic |
| 2 | JWT Config & Auth Middleware | `plan-task-executor` | N/A | Infrastructure setup |
| 2 | API Controllers | `plan-task-executor` | `code-style-reviewer` | REST endpoints |
| 2 | Backend Review | `code-principles-reviewer` | `code-style-reviewer` | Full backend validation |
| 3 | Frontend Architecture | `work-plan-architect` | `codebase-researcher` | Blazor component structure |
| 3 | Login/Callback Pages | `plan-task-executor` | `code-style-reviewer` | UI components |
| 3 | AuthService Frontend | `plan-task-executor` | `code-principles-reviewer` | Token management logic |
| 3 | HttpClientHandler | `plan-task-executor` | N/A | Middleware pattern |
| 3 | Frontend Review | `code-principles-reviewer` | `code-style-reviewer` | Full frontend validation |
| 4 | Google OAuth Config | `plan-task-executor` | N/A | Procedural setup |
| 5 | Test Implementation | `test-healer` | N/A | Write and validate tests |
| 5 | Security Review | `code-principles-reviewer` | N/A | SOLID principles + security |
| Final | Pre-Completion Validation | `pre-completion-validator` | N/A | Verify against requirements |

---

## Execution Schedule

### **Week 1: Database & Backend**

#### Day 1-2: Database Schema (Phase 1)
```
PARALLEL EXECUTION:
├─ codebase-researcher: Analyze current User model & DbContext
│  └─ Output: Architecture document of current DB schema
├─ Explore: Find all User-related files
│  └─ Output: File map of User model locations
└─ Result: Consolidated DB schema changes needed
```

**Tasks**:
- [ ] `codebase-researcher` analyzes EmailFixerDbContext and User model
- [ ] `Explore` finds all User references in codebase
- [ ] `plan-task-executor` creates and applies EF Core migration
- [ ] Verify migration runs on local database

#### Day 3-4: Backend Auth Service (Phase 2)
```
SEQUENTIAL EXECUTION:
├─ work-plan-architect: Design complete auth flow
│  ├─ GoogleTokenResponse model
│  ├─ AuthService interface & implementation
│  ├─ JWT token generation
│  └─ Output: Detailed task breakdown
│
├─ plan-task-executor (Task 1): GoogleTokenResponse + JWT Settings
├─ plan-task-executor (Task 2): AuthService.GoogleCallbackAsync
├─ plan-task-executor (Task 3): AuthService.GenerateJwtToken
├─ plan-task-executor (Task 4): AuthService helper methods
│
├─ code-principles-reviewer: Check AuthService SOLID principles
│  └─ Ensure: Single Responsibility, Dependency Injection
│
└─ code-style-reviewer: Format and naming conventions
   └─ Ensure: C# style guide compliance
```

**Tasks**:
- [ ] `work-plan-architect` creates detailed backend auth design
- [ ] `plan-task-executor` implements GoogleTokenResponse model
- [ ] `plan-task-executor` implements AuthService with all methods
- [ ] `code-principles-reviewer` validates implementation
- [ ] `code-style-reviewer` ensures code quality

#### Day 5: Backend API & Middleware (Phase 2 cont.)
```
SEQUENTIAL EXECUTION:
├─ plan-task-executor (Task 1): AuthController endpoints
├─ plan-task-executor (Task 2): Program.cs JWT configuration
├─ plan-task-executor (Task 3): Add [Authorize] to protected endpoints
│
├─ code-style-reviewer: API naming conventions
└─ code-principles-reviewer: Auth flow validation
```

**Tasks**:
- [ ] `plan-task-executor` implements AuthController
- [ ] `plan-task-executor` configures JWT in Program.cs
- [ ] `plan-task-executor` adds [Authorize] attributes
- [ ] Both reviewers validate backend implementation

### **Week 2: Frontend & Google Setup**

#### Day 1-2: Frontend Structure (Phase 3)
```
PARALLEL EXECUTION:
├─ work-plan-architect: Design Blazor auth component structure
│  └─ Output: Component hierarchy and data flow
├─ codebase-researcher: Analyze existing Blazor services
│  └─ Output: Service patterns used in project
└─ Explore: Find service interfaces and implementations
   └─ Output: Service location map
```

**Tasks**:
- [ ] `work-plan-architect` designs frontend auth structure
- [ ] `codebase-researcher` analyzes existing service patterns
- [ ] `Explore` maps service file locations

#### Day 3-4: Frontend Components (Phase 3 cont.)
```
SEQUENTIAL EXECUTION:
├─ plan-task-executor (Task 1): Login.razor page
├─ plan-task-executor (Task 2): AuthCallback.razor page
├─ plan-task-executor (Task 3): AuthService (frontend)
├─ plan-task-executor (Task 4): AuthHttpClientHandler
│
├─ code-style-reviewer: Razor component formatting
│  └─ HTML/C# style consistency
│
└─ code-principles-reviewer: Service architecture
   └─ DRY principle, no duplication
```

**Tasks**:
- [ ] `plan-task-executor` implements Login.razor
- [ ] `plan-task-executor` implements AuthCallback.razor
- [ ] `plan-task-executor` implements frontend AuthService
- [ ] `plan-task-executor` implements AuthHttpClientHandler
- [ ] Both reviewers validate frontend quality

#### Day 5: Integration & Google Setup (Phase 4)
```
SEQUENTIAL EXECUTION:
├─ plan-task-executor: Update MainLayout navigation
├─ plan-task-executor: Update Program.cs for frontend services
├─ plan-task-executor: Google OAuth credential setup guide
└─ code-style-reviewer: Verify all code quality
```

**Tasks**:
- [ ] `plan-task-executor` updates MainLayout
- [ ] `plan-task-executor` configures frontend services
- [ ] Create Google OAuth credentials (manual process)
- [ ] Update environment variables
- [ ] `code-style-reviewer` final frontend validation

### **Week 3: Testing & Deployment**

#### Day 1-2: Test Implementation (Phase 5)
```
SEQUENTIAL EXECUTION:
├─ test-healer: Create and validate test suite
│  ├─ Unit tests for AuthService
│  ├─ Integration tests for OAuth flow
│  ├─ E2E tests with Playwright
│  └─ Output: Test execution report
│
└─ code-principles-reviewer: Test quality & coverage
   └─ Ensure: Tests follow AAA pattern, good naming
```

**Tasks**:
- [ ] `test-healer` implements unit tests
- [ ] `test-healer` implements integration tests
- [ ] `test-healer` implements E2E tests
- [ ] Verify all tests passing
- [ ] `code-principles-reviewer` validates test quality

#### Day 3: Security & Final Review (Phase 5 cont.)
```
SEQUENTIAL EXECUTION:
├─ code-principles-reviewer: Security review
│  ├─ PKCE implementation
│  ├─ Token storage security
│  ├─ HTTPS enforcement
│  ├─ CORS configuration
│  └─ Output: Security audit report
│
└─ code-style-reviewer: Final code quality check
```

**Tasks**:
- [ ] `code-principles-reviewer` security audit
- [ ] `code-style-reviewer` final code review
- [ ] Fix any identified issues

#### Day 4-5: Completion & Deployment
```
SEQUENTIAL EXECUTION:
├─ pre-completion-validator: Validate against requirements
│  ├─ Check all user stories completed
│  ├─ Verify functionality working
│  ├─ Confirm no regressions
│  └─ Output: Completion report
│
└─ plan-task-executor: Deploy to Cloud Run
   └─ Update GitHub Actions workflow
   └─ Deploy API and Client
   └─ Verify in production
```

**Tasks**:
- [ ] `pre-completion-validator` validates all requirements
- [ ] Fix any identified gaps
- [ ] Push to GitHub
- [ ] GitHub Actions deploys to Cloud Run
- [ ] Verify production deployment

---

## Execution Sequence Details

### Key Dependencies
```
DB Schema Changes (Phase 1)
        ↓
Backend AuthService (Phase 2)
        ├→ Cannot start frontend without backend endpoints
        └→ Blocks Phase 3 start
        ↓
Frontend AuthService (Phase 3)
        ├→ Depends on AuthService being complete
        └→ Blocks Phase 4
        ↓
Google OAuth Setup (Phase 4)
        ├→ Needs client ID + secret
        └→ Blocks full testing
        ↓
Testing & Deployment (Phase 5)
        ├→ All phases must be complete
        └→ Final validation gate
```

### Parallelization Opportunities
```
WEEK 1:
├─ Day 1-2: DB research can happen while team reviews architecture
├─ Day 3-5: Backend can be split across multiple plan-task-executors
│           (AuthService in one, Controllers in another)
│
WEEK 2:
├─ Day 1-2: Frontend planning can happen immediately after DB
├─ Day 3-4: Frontend components can be split across executors
│           (LoginPage, AuthCallback, Services in parallel)
│
WEEK 3:
├─ Day 1-2: Tests can be written while reviewers audit code
├─ Day 3-5: All final steps are sequential (validation gates)
```

---

## Agent Communication Protocol

### Phase Handoff
Each phase ends with:
1. **Completion Report**: What was delivered
2. **Quality Metrics**: Code coverage, test results
3. **Blockers**: Any issues for next phase
4. **Recommendations**: Suggestions for next phase

### Quality Gates
- ✅ Code passes style review
- ✅ Code follows SOLID principles
- ✅ All tests passing
- ✅ No security vulnerabilities
- ✅ Architecture matches plan

### Failure Recovery
If `plan-task-executor` fails:
1. `code-principles-reviewer` identifies root cause
2. Document issue in task
3. Re-execute with feedback
4. Max 2 retry attempts before escalation

---

## Expected Deliverables

### Phase 1 Output
- ✅ EF Core migration file
- ✅ Updated User model
- ✅ Database schema verification

### Phase 2 Output
- ✅ AuthService.cs (450+ lines)
- ✅ AuthController.cs (200+ lines)
- ✅ JwtSettings & configuration
- ✅ Program.cs updates
- ✅ Unit tests (300+ lines)
- ✅ Integration tests (400+ lines)

### Phase 3 Output
- ✅ Login.razor page
- ✅ AuthCallback.razor page
- ✅ AuthService.cs (frontend)
- ✅ AuthHttpClientHandler.cs
- ✅ Updated MainLayout.razor
- ✅ E2E tests (500+ lines)

### Phase 4 Output
- ✅ Google OAuth credentials
- ✅ Secret Manager setup
- ✅ GitHub Actions workflow updates
- ✅ Environment variables documentation

### Phase 5 Output
- ✅ All tests passing
- ✅ Security audit passed
- ✅ Code coverage >80%
- ✅ Production deployment successful

---

## Success Criteria

By end of execution:

- ✅ User can login with Google OAuth
- ✅ JWT token generated and stored
- ✅ API endpoints require [Authorize]
- ✅ User stays logged in on page refresh
- ✅ User can logout
- ✅ Payments linked to authenticated user
- ✅ Credit history persists per user
- ✅ All tests passing (>80% coverage)
- ✅ No security vulnerabilities
- ✅ Production deployment successful
- ✅ Code follows SOLID + project conventions
- ✅ Full documentation created

---

## Rollback Plan

If critical issues found:
1. Stop deployment
2. `code-principles-reviewer` identifies root cause
3. `plan-task-executor` fixes issue
4. All tests must pass before retry
5. Max 2 iterations before escalation to manual review

---

## Timeline Estimation

| Phase | Days | Agents | Status |
|-------|------|--------|--------|
| 1 | 2 | 3 | Ready |
| 2 | 3 | 4 | Ready |
| 3 | 3 | 4 | Ready |
| 4 | 1 | 1 | Ready |
| 5 | 2 | 2 | Ready |
| **Total** | **11** | **Max 4 parallel** | **Ready** |

**Actual execution with parallelization: ~8-10 business days**

---

## Next Steps

1. ✅ Architecture plan created (OAUTH_ARCHITECTURE.md)
2. ✅ Implementation plan created (this file)
3. 🚀 **EXECUTE PHASE 1**: DB schema changes
4. 🚀 **EXECUTE PHASE 2**: Backend auth service
5. 🚀 **EXECUTE PHASE 3**: Frontend auth components
6. 🚀 **EXECUTE PHASE 4**: Google OAuth setup
7. 🚀 **EXECUTE PHASE 5**: Testing & deployment

