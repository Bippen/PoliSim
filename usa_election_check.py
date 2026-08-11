"""Reproduction and behaviour check for the USA vertical slice.
Ports NationalVoteModel + UnitedStatesElections to Python and asserts against seeded real results."""
GOP, DEM, LIB, OTH = "us-gop", "us-dem", "us-lib", "us-oth"
BASE = {GOP: 0.4975, DEM: 0.4719, LIB: 0.0047, OTH: 0.0259}   # sums to 1.0000
APPEAL = {GOP: [0.78,0.95,1.08,1.12], DEM: [1.24,1.06,0.93,0.88], LIB: [1.30,1.10,0.90,0.70], OTH: [1.15,1.05,0.95,0.85]}
COH_SHARE = [0.12,0.34,0.32,0.22]
PRES_T    = [0.435,0.585,0.697,0.757]
MID_T     = [0.22,0.375,0.52,0.625]
GAP = 1.06
W = dict(appr=0.003, grow=0.010, unem=-0.008, infl=-0.006, time=-0.004)

def swing(appr=50, g=2.0, u=5.0, i=2.0, yrs=0.0):
    return ((appr-50)*W['appr'] + (g-2.0)*W['grow'] + (u-5.0)*W['unem']
            + abs(i-2.0)*W['infl'] + yrs*W['time'])

def project(inc, midterm, **kw):
    s = swing(**kw)
    opp = sum(v for k,v in BASE.items() if k != inc)
    raw = {k: (v + s if k == inc else v - s*(v/opp if opp>0 else 0)) for k,v in BASE.items()}
    raw = {k: max(0.001, min(0.95, v)) for k,v in raw.items()}
    turn = MID_T if midterm else PRES_T
    w, ref = {k: 0.0 for k in BASE}, {k: 0.0 for k in BASE}
    for i2 in range(4):
        cw  = COH_SHARE[i2]*turn[i2]
        rw  = COH_SHARE[i2]*PRES_T[i2]          # reference: the pattern the baseline already embeds
        for k in BASE:
            a = APPEAL[k][i2]
            if midterm: a *= (1/GAP if k == inc else GAP)
            w[k]   += a*cw
            ref[k] += APPEAL[k][i2]*rw
    out = {k: raw[k]*(w[k]/ref[k] if ref[k] > 0 else 1.0) for k in BASE}
    t = sum(out.values())
    return {k: v/t for k,v in out.items()}

def two_party(sh, p): return sh[p]/(sh[GOP]+sh[DEM])
ANCHOR = 0.4975/(0.4975+0.4719)
def outcome(vs, ratio, av, ao):
    bias = ao - (0.5 + ratio*(av-0.5))
    return max(0.02, min(0.98, 0.5 + ratio*(vs-0.5) + bias))
def house(sh): 
    g = round(435*outcome(two_party(sh,GOP), 2.0, ANCHOR, 220/435))
    return {GOP:g, DEM:435-g}
def ec(sh):
    g = round(538*outcome(two_party(sh,GOP), 3.4, ANCHOR, 312/538))
    return {GOP:g, DEM:538-g}

fails = []
def check(name, cond, detail=""):
    print(f"  {'PASS' if cond else 'FAIL'}  {name}{'  -- '+detail if detail else ''}")
    if not cond: fails.append(name)

print("1. REPRODUCTION - does the seed return the real 2024 result?")
neutral = dict(BASE)
h, e = house(neutral), ec(neutral)
check("House 220 R / 215 D", h=={GOP:220,DEM:215}, str(h))
check("Electoral College 312 R / 226 D", e=={GOP:312,DEM:226}, str(e))
check("House seats sum to 435", sum(h.values())==435)
check("Electors sum to 538", sum(e.values())==538)

print("\n2. TURNOUT - does the MODELLED cohort curve reproduce the VERIFIED nationals?")
pres = sum(s*t for s,t in zip(COH_SHARE,PRES_T)); mid = sum(s*t for s,t in zip(COH_SHARE,MID_T))
check("presidential turnout ~64.1%", abs(pres-0.641)<0.005, f"got {pres:.4f}")
check("midterm turnout ~46%", abs(mid-0.46)<0.005, f"got {mid:.4f}")
check("cohort shares sum to 1", abs(sum(COH_SHARE)-1)<1e-9)
check("seeded party shares sum to 1", abs(sum(BASE.values())-1)<1e-9, f"sum={sum(BASE.values()):.4f}")

print("\n3. MIDTERM SIGN - president's party must lose House seats WHICHEVER party it is")
for inc,label in ((GOP,"Republican"),(DEM,"Democratic")):
    base_h = house(project(inc, False))[inc]
    mid_h  = house(project(inc, True))[inc]
    check(f"{label} president loses House seats at midterm", mid_h < base_h,
          f"{base_h} -> {mid_h} ({mid_h-base_h:+d})")

print("\n4. BOUNDS - shares stay sane at the extremes")
for appr in (0,15,35,50,65,85,100):
    sh = project(GOP, False, appr=appr)
    ok = abs(sum(sh.values())-1)<1e-9 and all(0<=v<=1 for v in sh.values())
    check(f"approval {appr}: shares valid and sum to 1", ok, f"R={sh[GOP]:.3f} D={sh[DEM]:.3f}")

print("\n4b. NEUTRAL IDENTITY - a presidential election at baseline must return the baseline")
nb = project(GOP, False)
check("neutral projection reproduces R 49.75%", abs(nb[GOP]-0.4975)<0.002, f"got {nb[GOP]:.4f}")
check("neutral projection reproduces D 47.19%", abs(nb[DEM]-0.4719)<0.002, f"got {nb[DEM]:.4f}")

print("\n5. DIRECTION - approval and the economy move the incumbent the right way")
lo, hi = project(GOP, False, appr=25), project(GOP, False, appr=75)
check("higher approval -> higher incumbent share", hi[GOP] > lo[GOP], f"{lo[GOP]:.4f} -> {hi[GOP]:.4f}")
rec, boom = project(GOP, False, g=-3.0, u=9.0), project(GOP, False, g=5.0, u=3.5)
check("boom beats recession for the incumbent", boom[GOP] > rec[GOP], f"{rec[GOP]:.4f} -> {boom[GOP]:.4f}")
defl = project(GOP, False, i=-1.0); tgt = project(GOP, False, i=2.0)
check("deflation is not a gift to the incumbent", defl[GOP] < tgt[GOP], f"{defl[GOP]:.4f} vs {tgt[GOP]:.4f}")

print(f"\n{'ALL CHECKS PASSED' if not fails else str(len(fails))+' FAILED: '+', '.join(fails)}")
