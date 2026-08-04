// Shared "worker pool" pattern: a fixed number of workers pull from a shared cursor until
// the list is exhausted, so at most `concurrency` calls to `worker` are ever in flight at
// once. Used for any bulk operation (upload, delete, reassign, ...) that fires one API call
// per item and shouldn't fire them all at the same time.
export async function runWithConcurrency<T>(
  items: T[],
  concurrency: number,
  worker: (item: T) => Promise<void>
): Promise<void> {
  let cursor = 0;

  async function runNext() {
    while (cursor < items.length) {
      const item = items[cursor++];
      await worker(item);
    }
  }

  await Promise.all(Array.from({ length: Math.min(concurrency, items.length) }, runNext));
}
