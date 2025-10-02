using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SoulLinkChain : MonoBehaviour
{
    private List<SoulLinkEnemy> members;
    private List<LineRenderer> segments;
    private Vector3[] lastKnownPositions; // stored when members die early
    private Material outlineMaterial;
    private LineRenderer linePrefab;
    private float lineSpeed;
    private float deathDelay; // kept for compatibility (not used for sequential waiting now)
    private bool isKilling = false;


    private static bool IsSoulLinkActive()
    {
        // Find both power-up manager instances in the scene.
        PowerUpManager player1Manager = FindObjectOfType<PowerUpManager>();
        PowerUpManagerL3antix player2Manager = FindObjectOfType<PowerUpManagerL3antix>();

        // Check if player 1 has the power-up.
        if (player1Manager != null && player1Manager.HasPowerUp(PowerUpType.SoulLink))
        {
            return true;
        }

        // Check if player 2 has the power-up.
        if (player2Manager != null && player2Manager.HasPowerUp(PowerUpType.SoulLink))
        {
            return true;
        }

        // If neither player has it, return false.
        return false;
    }
    /// <summary>
    /// Factory: create chain manager and start building the chain.
    /// </summary>
    public static SoulLinkChain CreateChain(SoulLinkEnemy startEnemy, int minLinks, int maxLinks,
                                          float maxLinkDistance, LineRenderer linePrefab,
                                          Material outlineMaterial, float lineSpeed, float deathDelay)
    {
        // --- THE FIX: Use the new helper function ---
        // OLD WAY: if (!PowerUpManager.SoulLinkEquipped)
        // NEW WAY:
        if (!IsSoulLinkActive())
            return null;
        // --- END OF THE FIX ---

        GameObject go = new GameObject("SoulLinkChain");
        SoulLinkChain chain = go.AddComponent<SoulLinkChain>();
        // ... (the rest of your CreateChain method is correct and stays the same) ...
        chain.linePrefab = linePrefab;
        chain.outlineMaterial = outlineMaterial;
        chain.lineSpeed = lineSpeed;
        chain.deathDelay = deathDelay;
        chain.StartCoroutine(chain.BuildChainAndAnimate(startEnemy, minLinks, maxLinks, maxLinkDistance));
        return chain;
    }

    private IEnumerator BuildChainAndAnimate(SoulLinkEnemy startEnemy, int minLinks, int maxLinks, float maxLinkDistance)
    {
        // Prepare lists
        members = new List<SoulLinkEnemy>();
        segments = new List<LineRenderer>();

        // 1) Build ordered chain: start -> closest -> next closest ... up to random(min,max)
        members.Add(startEnemy);
        startEnemy.inChain = true;
        startEnemy.chain = this;

        if (startEnemy == null || startEnemy.linePoint == null || linePrefab == null)
        {
            // unflag any partial inChain
            foreach (var e in members)
                if (e != null) { e.inChain = false; e.chain = null; }
            Destroy(gameObject);
            yield break;
        }
        int linksWanted = Random.Range(minLinks, maxLinks + 1);
        SoulLinkEnemy current = startEnemy;

        for (int i = 0; i < linksWanted; i++)
        {
            SoulLinkEnemy next = FindClosestUnchained(current, maxLinkDistance);
            if (next == null) break;

            members.Add(next);
            next.inChain = true;
            next.chain = this;
            current = next;
            yield return null; // allow frame to breathe
        }

        // If chain only contains single member, release and exit (nothing to link)
        if (members.Count < 2)
        {
            foreach (var e in members)
            {
                if (e != null) { e.inChain = false; e.chain = null; }
            }
            Destroy(gameObject);
            yield break;
        }

        // Prepare last known positions array (same length as members)
        lastKnownPositions = new Vector3[members.Count];
        for (int p = 0; p < members.Count; p++)
        {
            SoulLinkEnemy me = members[p];
            if (me != null && me.linePoint != null)
                lastKnownPositions[p] = me.linePoint.position;
            else if (me != null)
                lastKnownPositions[p] = me.transform.position;
            else
                lastKnownPositions[p] = Vector3.zero; // defensive, should rarely happen
        }
        if (outlineMaterial != null)
        {
            for (int i = 0; i < members.Count; i++)
            {
                SoulLinkEnemy e = members[i];
                if (e != null)
                {
                    e.SetOutlineMaterial(outlineMaterial);

                    // If somehow sprite renderer lost its mat, re-apply
                    if (e.spriteRenderers != null)
                    {
                        foreach (var sr in e.spriteRenderers)
                        {
                            if (sr != null && sr.material != outlineMaterial)
                                sr.material = outlineMaterial;
                        }
                    }
                }
            }
        }

        // 2) For each segment instantiate line and animate the line "growing" from source to target
        for (int i = 0; i < members.Count - 1; i++)
        {
            SoulLinkEnemy a = members[i];
            SoulLinkEnemy b = members[i + 1];

            // instantiate a line renderer (segment)
            Vector3 segSpawnPos = (a != null && a.linePoint != null) ? a.linePoint.position : Vector3.zero;
            LineRenderer seg = Instantiate(linePrefab, segSpawnPos, Quaternion.identity);
            seg.transform.SetParent(this.transform, true);
            seg.positionCount = 2;
            seg.useWorldSpace = true;
            segments.Add(seg);

            // animate the end point growing toward target
            Vector3 startPos = a != null ? a.linePoint.position : lastKnownPositions[i];
            Vector3 targetPos = b != null ? b.linePoint.position : lastKnownPositions[i + 1];

            float distance = Vector3.Distance(startPos, targetPos);
            float duration = Mathf.Max(0.001f, distance / lineSpeed);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                Vector3 lerpPos = Vector3.Lerp(startPos, targetPos, t);
                seg.SetPosition(0, startPos);
                seg.SetPosition(1, lerpPos);

                // update startPos and targetPos so the growing animation follows moving enemies
                startPos = a != null ? a.linePoint.position : startPos;
                targetPos = b != null ? b.linePoint.position : targetPos;

                yield return null;
            }

            // snap final (ensure exact)
            if (a != null && b != null)
            {
                seg.SetPosition(0, a.linePoint.position);
                seg.SetPosition(1, b.linePoint.position);
            }

            // tiny pause so visuals read clearly (very short)
            yield return new WaitForSeconds(0.03f);
        }

        // 3) Keep segments updating so they follow the moving enemies
        StartCoroutine(UpdateSegmentsRoutine());
        StartCoroutine(EnsureOutlineRoutine());

    }

    private SoulLinkEnemy FindClosestUnchained(SoulLinkEnemy from, float maxDistance)
    {
        SoulLinkEnemy closest = null;
        float best = float.MaxValue;
        foreach (var e in FindObjectsOfType<SoulLinkEnemy>())
        {
            if (e == null || e == from) continue;
            if (e.inChain) continue; // already chained
            if (e.linePoint == null) continue; // ignore invalid enemies
            float d = Vector2.Distance(from.transform.position, e.transform.position);
            if (d <= maxDistance && d < best)
            {
                best = d;
                closest = e;
            }
        }
        return closest;
    }


    private IEnumerator UpdateSegmentsRoutine()
    {
        while (true)
        {
            if (members == null || segments == null) break;

            // 🔥 New: if all members are gone or destroyed, cleanup immediately
            bool allGone = true;
            foreach (var m in members)
            {
                if (m != null) { allGone = false; break; }
            }
            if (allGone)
            {
                CleanupAndFinish();
                yield break;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                LineRenderer lr = segments[i];
                if (lr == null) continue;

                if (i >= members.Count - 1)
                {
                    Destroy(lr.gameObject);
                    segments[i] = null;
                    continue;
                }

                SoulLinkEnemy a = members[i];
                SoulLinkEnemy b = members[i + 1];

                Vector3 aPos = (a != null && a.linePoint != null) ? a.linePoint.position : lastKnownPositions[i];
                Vector3 bPos = (b != null && b.linePoint != null) ? b.linePoint.position : lastKnownPositions[i + 1];

                if (aPos == Vector3.zero && bPos == Vector3.zero)
                {
                    Destroy(lr.gameObject);
                    segments[i] = null;
                    continue;
                }

                lr.SetPosition(0, aPos);
                lr.SetPosition(1, bPos);
            }

            yield return null;
        }
    }


    /// <summary>
    /// Called by a member's health.Die() at the start, before destruction.
    /// This records the last known position and starts the kill sequence
    /// (if not already running) from the next member.
    /// </summary>
    public void OnMemberDied(SoulLinkEnemy dead)
    {
        if (members == null) return;
        int index = members.IndexOf(dead);
        if (index < 0) return;

        // store last known position BEFORE we null the member
        if (dead != null && dead.linePoint != null)
            lastKnownPositions[index] = dead.linePoint.position;
        else if (dead != null)
            lastKnownPositions[index] = dead.transform.position;

        // mark the member slot as gone so UpdateSegmentsRoutine falls back to lastKnownPositions
        members[index] = null;

        // clear that dead's chain flags so it won't re-notify
        if (dead != null)
        {
            dead.inChain = false;
            dead.chain = null;
        }

        // if kill sequence already in progress ignore starting another
        if (isKilling) return;

        // start outward sequential kill from the dead index
        isKilling = true;
        StartCoroutine(KillWholeChain(index));
    }


    private IEnumerator KillWholeChain(int deadIndex)
    {
        if (members == null) yield break;

        // Build lists of indices to the right and left (only alive slots)
        List<int> right = new List<int>();
        for (int i = deadIndex + 1; i < members.Count; i++)
            if (members[i] != null) right.Add(i);

        List<int> left = new List<int>();
        for (int i = deadIndex - 1; i >= 0; i--)
            if (members[i] != null) left.Add(i);

        int ri = 0, li = 0;

        // alternate outward: right[0], left[0], right[1], left[1], ...
        while (ri < right.Count || li < left.Count)
        {
            if (ri < right.Count)
            {
                int targetIndex = right[ri];
                int fromIndex = targetIndex - 1; // segment connecting fromIndex -> targetIndex
                yield return StartCoroutine(KillStep(fromIndex, targetIndex));
                ri++;
            }

            if (li < left.Count)
            {
                int targetIndex = left[li];
                int fromIndex = targetIndex + 1; // segment connecting targetIndex -> fromIndex
                yield return StartCoroutine(KillStep(fromIndex, targetIndex));
                li++;
            }
        }

        // done
        CleanupAndFinish();
    }


    private IEnumerator EnsureOutlineRoutine()
    {
        while (members != null)
        {
            foreach (var e in members)
            {
                if (e == null) continue;
                e.SetOutlineMaterial(outlineMaterial);

                // re-apply in case something overwrote it
                if (e.spriteRenderers != null)
                {
                    foreach (var sr in e.spriteRenderers)
                    {
                        if (sr != null && sr.material != outlineMaterial)
                            sr.material = outlineMaterial;
                    }
                }
            }
            yield return null; // every frame
        }
    }


    /// <summary>
    /// Sequentially kills forward from startIndex, then backward from the dead index's left side.
    /// The method waits for each victim to actually be destroyed before continuing, so kills are truly sequential.
    /// </summary>
    // New: sequential kill that expands outward from the deadIndex inward both directions
    private IEnumerator SequentialKillFrom(int deadIndex)
    {
        isKilling = true;

        // kill to the right (deadIndex+1 .. end), one-by-one shrinking segment
        for (int right = deadIndex + 1; right < members.Count; right++)
        {
            yield return StartCoroutine(KillStep(right - 1, right));
        }

        // kill to the left (deadIndex-1 .. 0), one-by-one shrinking segment
        for (int left = deadIndex - 1; left >= 0; left--)
        {
            yield return StartCoroutine(KillStep(left + 1, left));
        }

        // finished
        CleanupAndFinish();
    }

    // Helper: animate segment between fromIndex -> toIndex shrinking, then force toIndex to die
    private IEnumerator KillStep(int fromIndex, int toIndex)
    {
        // sanity checks
        if (members == null || segments == null) yield break;
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= members.Count || toIndex >= members.Count) yield break;

        // compute segment index (min of from/to)
        int segIndex = Mathf.Min(fromIndex, toIndex);
        LineRenderer seg = (segIndex >= 0 && segIndex < segments.Count) ? segments[segIndex] : null;

        // compute start and end points (use lastKnownPositions if a member null)
        Vector3 startPos = (members[fromIndex] != null && members[fromIndex].linePoint != null) ? members[fromIndex].linePoint.position : lastKnownPositions[fromIndex];
        Vector3 endPos = (members[toIndex] != null && members[toIndex].linePoint != null) ? members[toIndex].linePoint.position : lastKnownPositions[toIndex];

        // animate shrink (if segment exists)
        if (seg != null)
        {
            float dur = Mathf.Max(0.001f, Vector3.Distance(startPos, endPos) / lineSpeed);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                Vector3 lerpStart = Vector3.Lerp(startPos, endPos, t);
                seg.SetPosition(0, lerpStart);
                seg.SetPosition(1, endPos);
                yield return null;
            }
            seg.SetPosition(0, endPos);
            seg.SetPosition(1, endPos);
        }

        // force victim to die (ForceDieFromChain clears its inChain before calling its Die)
        SoulLinkEnemy victim = members[toIndex];
        if (victim != null)
        {
            // clear the member slot immediately so UpdateSegments falls back to lastKnownPositions
            members[toIndex] = null;
            victim.ForceDieFromChain();

            // wait until destroyed
            yield return new WaitUntil(() => victim == null);
        }
        else
        {
            // nothing to do (already null)
            yield return null;
        }
    }


    private void CleanupAndFinish()
    {
        // Destroy lines cleanly with a short fade
        if (segments != null)
        {
            float fadeTime = 0.25f;
            // fade coroutine
            StartCoroutine(FadeAndDestroySegments(segments, fadeTime));
            segments = null;
        }

        // restore materials and release inChain flag
        if (members != null)
        {
            foreach (var m in members)
            {
                if (m == null) continue;
                m.RestoreOriginalMaterials();
                m.inChain = false;
                m.chain = null;
            }
            members.Clear();
            members = null;
        }

        // Destroy this chain object after a short delay (allow fade coroutine to complete)
        Destroy(gameObject, 0.3f);
    }

    private IEnumerator FadeAndDestroySegments(List<LineRenderer> segs, float fadeTime)
    {
        if (segs == null) yield break;
        float elapsed = 0f;

        // capture initial colors (if any)
        Color[] startColors = new Color[segs.Count];
        for (int i = 0; i < segs.Count; i++)
        {
            var s = segs[i];
            startColors[i] = (s != null) ? s.startColor : Color.white;
        }

        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            for (int i = 0; i < segs.Count; i++)
            {
                var s = segs[i];
                if (s == null) continue;
                Color sc = startColors[i];
                Color c = Color.Lerp(sc, new Color(sc.r, sc.g, sc.b, 0f), t);
                s.startColor = c;
                s.endColor = c;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // final destroy
        for (int i = 0; i < segs.Count; i++)
        {
            if (segs[i] != null) Destroy(segs[i].gameObject);
        }
    }
}
