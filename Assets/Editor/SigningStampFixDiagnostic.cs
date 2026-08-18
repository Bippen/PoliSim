using System;
using System.Reflection;
using PoliSim.Data;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PLAYTEST FIX VERIFICATION (2026-08-18): calls the real `SigningScreen.Build` directly for a
    /// PASSED and a FAILED `DivisionRecord`, then inspects the resulting GameObject hierarchy - no
    /// screen, no driver, no country-selector/scenario machinery to desync. Structural assertions
    /// (which child exists, what the button's Label text reads) are a more precise bar for THIS fix
    /// than a screenshot: the defect was about which objects get created, not about pixels.
    /// </summary>
    public static class SigningStampFixDiagnostic
    {
        [MenuItem("PoliSim/Run Signing Stamp Fix Diagnostic")]
        private static void RunFromMenu() => Run();

        public static void Run()
        {
            int failures = 0;

            failures += CheckOne(passed: true);
            failures += CheckOne(passed: false);

            if (failures == 0)
            {
                Debug.Log("SIGNSTAMP: all assertions PASSED - the seal/button now match record.Passed in both branches.");
            }
            else
            {
                Debug.LogError($"SIGNSTAMP: {failures} assertion(s) FAILED.");
            }

            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int CheckOne(bool passed)
        {
            int failures = 0;
            string tag = passed ? "PASSED" : "FAILED";

            World world = WorldFactory.CreateDefault();
            Country country = world.GetCountry(CountryId.USA);

            var record = new DivisionRecord
            {
                Number = 1,
                Title = $"SIGNSTAMP test bill ({tag})",
                Date = DateTime.Now,
                Alignment = passed ? 0.30f : -0.30f,
                Passed = passed
            };

            SigningScreen screen = SigningScreen.Build(country, record, () => { });
            if (screen == null || screen.Root == null)
            {
                Debug.LogError($"SIGNSTAMP[{tag}]: SigningScreen.Build returned null - furniture missing, cannot assert.");
                return 1;
            }

            try
            {
                Transform sealLanding = FindDeep(screen.Root.transform, "SealLanding");
                if (sealLanding == null)
                {
                    Debug.LogError($"SIGNSTAMP[{tag}]: no SealLanding found at all.");
                    return failures + 1;
                }

                Transform sealImageGo = sealLanding.Find("Seal");
                Transform sealTimerGo = sealLanding.Find("SealTimer");

                if (passed)
                {
                    if (sealImageGo == null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: expected a 'Seal' child (the wax seal) - not found.");
                        failures++;
                    }
                    else
                    {
                        Image sealImage = sealImageGo.GetComponent<Image>();
                        if (sealImage == null || sealImage.sprite == null)
                        {
                            Debug.LogError($"SIGNSTAMP[{tag}]: 'Seal' child has no Image/sprite.");
                            failures++;
                        }
                        else
                        {
                            Debug.Log($"SIGNSTAMP[{tag}]: seal Image present, sprite={sealImage.sprite.name}. OK.");
                        }
                    }

                    if (sealTimerGo != null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: unexpected 'SealTimer' child present on a PASSED record (should only exist for FAILED).");
                        failures++;
                    }
                }
                else
                {
                    if (sealImageGo != null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: a 'Seal' child (the official wax seal) exists for a REJECTED division - this is exactly the bug that was fixed.");
                        failures++;
                    }
                    else
                    {
                        Debug.Log($"SIGNSTAMP[{tag}]: no 'Seal' child - correct, nothing was enacted.");
                    }

                    if (sealTimerGo == null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: no 'SealTimer' child - the settle-timer contract (Sign()/Sealed) would be broken for a rejected division.");
                        failures++;
                    }
                    else
                    {
                        Debug.Log($"SIGNSTAMP[{tag}]: 'SealTimer' present, carries the settle beat with nothing visible. OK.");
                    }
                }

                // The SealDrop component must exist on WHICHEVER object was created (Seal or
                // SealTimer), and it must start inactive (activates only on Sign()).
                GameObject sealBeatGo = passed ? sealImageGo?.gameObject : sealTimerGo?.gameObject;
                if (sealBeatGo != null)
                {
                    if (sealBeatGo.GetComponent<SealDrop>() == null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: the seal-beat object has no SealDrop component - Sign()/Sealed contract broken.");
                        failures++;
                    }
                    if (sealBeatGo.activeSelf)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: the seal-beat object starts ACTIVE - should start inactive until Sign() is clicked.");
                        failures++;
                    }
                }

                Transform button = FindDeep(screen.Root.transform, "SignButton");
                if (button == null)
                {
                    Debug.LogError($"SIGNSTAMP[{tag}]: no SignButton found.");
                    failures++;
                }
                else
                {
                    Transform label = button.Find("Label");
                    Text labelText = label != null ? label.GetComponent<Text>() : null;
                    if (labelText == null)
                    {
                        Debug.LogError($"SIGNSTAMP[{tag}]: SignButton has no Label Text component.");
                        failures++;
                    }
                    else
                    {
                        string expected = passed ? "SIGN" : "FILE";
                        if (labelText.text != expected)
                        {
                            Debug.LogError($"SIGNSTAMP[{tag}]: button reads '{labelText.text}', expected '{expected}'.");
                            failures++;
                        }
                        else
                        {
                            Debug.Log($"SIGNSTAMP[{tag}]: button reads '{labelText.text}'. OK.");
                        }
                    }
                }

                // The plate's own stamp (CARRIED/REJECTED) - confirm it is still correctly wired,
                // unaffected by this fix (it always was correct; asserting it here catches a future
                // regression touching the same file).
                Transform stamp = FindDeep(screen.Root.transform, "Stamp");
                if (stamp == null)
                {
                    Debug.LogError($"SIGNSTAMP[{tag}]: no 'Stamp' (CARRIED/REJECTED plate stamp) found.");
                    failures++;
                }
                else
                {
                    Debug.Log($"SIGNSTAMP[{tag}]: plate Stamp present. OK.");
                }
            }
            finally
            {
                screen.Destroy();
            }

            return failures;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
