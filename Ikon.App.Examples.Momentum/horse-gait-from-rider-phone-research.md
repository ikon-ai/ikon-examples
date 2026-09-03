# Horse gait from a rider's phone

Investigated 2026-08-25, for Momentum's gait detection. What the literature establishes, what it does
not cover, and what that means for a phone in a rider's pocket. Check the date before trusting it.

## The question

Momentum currently segments horse gaits by **speed band** with hysteresis. That is a placeholder and
it is wrong in the case that matters: a collected canter and an extended trot cover ground at the same
rate, so speed cannot separate *ravi* from *laukka* at the boundary. The discriminator is the beat.

| Gait | Beats | Structure |
|---|---|---|
| Käynti (walk) | 4 | lateral sequence, no suspension |
| Ravi (trot) | 2 | diagonal pairs, symmetric, with suspension |
| Laukka (canter) | 3 | asymmetric, has a lead, with suspension |
| Neliravi (gallop) | 4 | asymmetric, with suspension |

Symmetry is the strongest signal: trot is symmetric between half-strides, canter is not. That
distinction survives being filtered through a saddle and a rider, which speed does not.

## Prior art — the field is mature

**Horse-mounted, the reference standard.** [Serra Bragança et al. 2020, *Scientific Reports*](https://www.nature.com/articles/s41598-020-73215-9)
classified eight gaits from 120 horses, seven IMUs, 7,576 strides, reaching **97 %** with an LSTM over
1–3 s windows. This is the accuracy ceiling and it needs sensors on the animal.

**Rider-mounted, which is our actual problem.** Three studies, and they are unexpectedly encouraging:

- [Phone in the rider's pocket, five-gaited Icelandic horses](https://pmc.ncbi.nlm.nih.gov/articles/PMC9817528/) —
  **94.4 %** with a Bi-LSTM. The most directly comparable work there is.
- [iPhone strapped to the rider's thigh](https://beva.onlinelibrary.wiley.com/doi/10.1111/evj.72_12595) —
  99.8 % frame-by-frame across walk, sitting trot, rising trot and canter. A fixed mount is a much
  easier problem than a pocket, but note it treated **rising and sitting trot as separate classes**,
  which is the right instinct.
- [Dedicated rider-worn accelerometers](https://pmc.ncbi.nlm.nih.gov/articles/PMC12024389/) at knee,
  back, chest and arm — **89.7 %**.

### What the pocket study actually did

Worth copying, because most of these were choices we would otherwise have to discover:

| Decision | Theirs |
|---|---|
| Rate | 50 Hz (higher-rate devices downsampled to it) |
| Inputs | 6-dim — three axes of acceleration **and** three of gyroscope |
| Window | **1.5 s**, tested 0.5–4 s; 90 % overlap |
| Frame | Rotated to a horse-frame using **GPS-derived heading**, which beat a world-frame |
| GPS speed as a feature | **No consistent improvement** |
| Model | Bi-LSTM 94.4 %, 1D CNN 93.9 %, LSTM 93.3 %, GRU 91.2 % |
| Smoothing | Exponential decay plus a 7-point majority vote over sequential predictions |
| Training | Transitions excluded — a 2 s window either side of each gait change |
| Data | 17 horses, 14 riders, 5.8 hours |

Per-gait accuracy: walk 97 %, canter 94 %, flying pace 93 %, tölt 89 %, **trot 82 %**.

Three findings that change our design:

1. **50 Hz and accelerometer-plus-gyroscope.** Momentum currently streams user-acceleration only.
   Gyroscope is half their input and needs turning on.
2. **The rider's technique is the dominant confusion.** Sitting, rising and two-point produce
   different signals for the same gait, and that — not the horse — drove their worst errors. A rising
   trot imposes a strong component at the *rider's* rhythm, one per two strides, on top of the horse's.
3. **GPS heading is a useful input, GPS speed is not.** We have both already. The heading is used to
   rotate the signal into the horse's frame so a phone's orientation in a pocket stops mattering.

**Our problem is easier than theirs.** Tölt and flying pace are the hard classes and they were
classifying five gaits including both. Finnish riding needs käynti, ravi, laukka and possibly
neliravi — four classes, none of them the ambiguous ones. Their trot confusion was specifically with
tölt.

## Real time or after the fact — both, and they are different jobs

The architecture Momentum already has answers this: live detectors during the outing, and an
authoritative full pass at the finish. Gaits should follow the same split.

**Live.** A 1.5 s window with 90 % overlap yields a call roughly every 150 ms, lagging the real
transition by about a window plus whatever the smoothing adds — call it two seconds. That is fine for
"you're in canter" on the live screen and for the coach. It cannot be better: a classifier cannot know
a stride has changed until it has seen a stride.

**After the finish.** A second pass over the stored track is strictly better and should be what the
log records, for reasons that do not apply live:

- **Future context.** A boundary is far easier to place when both sides are known. The transition
  windows the paper *excluded from training* are exactly what a post pass can resolve.
- **Transition structure.** Gaits do not change arbitrarily — walk↔trot↔canter, rarely walk→canter —
  and they have plausible durations. A Viterbi pass over per-window posteriors with a transition
  matrix cleans up isolated misclassifications that no causal smoother can.
- **Per-horse normalisation.** With the whole ride in hand you can standardise features against that
  ride's own distribution, which removes most of the phone-placement and rider-technique variance.

So: live for the screen, post-process for the record. Where they disagree, the record wins.

## Self-learning — the labels are the whole problem

Every study above got its labels from **sensors on the horse** or from video judged by experts. We
have neither. This, not the classifier, is what stands between us and gait detection.

Four options, roughly in order of how soon they pay off:

1. **Rider labelling, a few taps.** During or after a ride the rider marks "this stretch was ravi".
   Even a minute or two per ride yields hundreds of labelled windows, and it is honest about what it
   is. Best first move.
2. **Cluster first, name once.** Unsupervised clustering (GMM or HDBSCAN) over windowed features
   finds the gaits as clusters without any labels — they are genuinely distinct in feature space. The
   rider then names each cluster once, per horse. This is attractive because it adapts to the horse
   *and* the rider's technique, which is the largest error source in the literature.
3. **Weak labels to bootstrap.** The existing speed-band segmentation is wrong at the boundaries but
   right in the middle of each gait. Train on the confident middles, and let the model resolve the
   boundaries that the heuristic never could.
4. **Self-supervised pretraining** on unlabelled riding, fine-tuned on a small labelled set. The
   standard answer when unlabelled data is plentiful and labels are scarce, which is exactly our
   position — but it needs a corpus we do not have yet, so it is a later step, not a first one.

Options 2 and 3 combine well: bootstrap with weak labels, cluster per horse, ask the rider to confirm
the cluster names once, and improve from there.

## What not to do

**Do not put an LLM on the raw signal.** Fifty hertz of three-axis acceleration is not a language
problem, and a model asked to classify it directly will be slower, costlier and worse than a small
1D CNN — which the paper measured at 93.9 %, within half a point of the best model it tried. This is
the same division Momentum already draws: the detectors measure, the AI narrates. The right jobs for
a model here are the labelling conversation ("was that a canter?"), and describing the ride once the
gaits are known.

## Suggested next steps

1. Turn on the gyroscope in Momentum's `MotionOptions` and store raw windows alongside the track —
   without a corpus nothing else is possible. Storage is cheap; unrecorded rides are gone.
2. Rotate to a horse-frame using the GPS heading we already record.
3. A rider-labelling affordance, however crude, to start accumulating ground truth.
4. Cluster the collected windows and see whether gaits fall out unsupervised, as the literature
   implies they should.
5. Only then a classifier — a 1D CNN, not a Bi-LSTM, until there is evidence the extra 0.5 % matters.
6. Viterbi smoothing in the post-finish pass, with a transition matrix over the four gaits.

## What was built after this

Steps 1 and part of 2 landed the same day, as `MotionCorpus` — the gyroscope is on for horse outings
and every real outing writes its raw stream to per-activity assets. See the "motion corpus" section of
`platform-dotnet/Ikon.App.Examples.Momentum/momentum-tracker-app-spec.md` for the as-built. Steps 3–6 — rider labelling,
clustering, a classifier, Viterbi smoothing — are not built.

## Sources

- [Serra Bragança et al., *Improving gait classification in horses by using IMU generated data and machine learning*, Scientific Reports 2020](https://www.nature.com/articles/s41598-020-73215-9)
- [*Efficient Development of Gait Classification Models for Five-Gaited Horses Based on Mobile Phone Sensors*, PMC9817528](https://pmc.ncbi.nlm.nih.gov/articles/PMC9817528/)
- [*Detecting Equine Gaits Through Rider-Worn Accelerometers*, PMC12024389](https://pmc.ncbi.nlm.nih.gov/articles/PMC12024389/)
- [*Automated Detection of Basic Equestrian Exercises Based on Smartphone Accelerometer Data*, Equine Veterinary Journal 2016](https://beva.onlinelibrary.wiley.com/doi/10.1111/evj.72_12595)
- [*Inertial Sensor Technologies — Their Role in Equine Gait Analysis, a Review*, PMC10386433](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC10386433/)
