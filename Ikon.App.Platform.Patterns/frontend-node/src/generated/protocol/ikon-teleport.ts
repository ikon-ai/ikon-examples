// noinspection JSUnusedGlobalSymbols

export const MinimumHeaderLength = 27;
export const ProtocolVersion = 1;
// Mirrors ProtocolMessage.MaxMessageSize: the ceiling on a message on the wire and on a payload once
// decompressed. The server refuses anything larger, so a larger one reaching a client came from
// something other than the server — a proxy it passed through, most likely.
export const MaxMessageSize = 20 * 1024 * 1024;

export type ProtocolMessage = Uint8Array & { readonly __brand: 'ProtocolMessage' }

export enum PayloadType {
  Unknown = 0,
  MessagePack = 1,
  MemoryPack = 2,
  Json = 4,
  Teleport = 8,
  All = 15,
}

export interface ProtocolMessageOverrides {
  trackId?: number;
  sequenceId?: number;
  flags?: number;
  targetIds?: readonly number[];
  payloadType?: PayloadType;
  compress?: boolean;
}

export const MessageFlag = {
  None: 0,
  SendBackToSender: 1,
  Delayable: 2,
  SendToUser: 4,
  Compressed: 8,
  Unreliable: 16,
} as const;

export interface ProtocolMessageHeaders {
  length: number;
  opcode: number;
  senderId: number;
  trackId: number;
  sequenceId: number;
  targetIds: number[];
  payloadVersion: number;
  payloadType: PayloadType;
  flags: number;
  /**
   * Set by the receiving client when the WebRTC data channel delivered this message, rather
   * than the reliable protocol channel. Absent on anything read straight off the wire -- the
   * flags byte records what the sender asked for, not the transport that ran.
   */
  viaDataChannel?: boolean;
}

export function asProtocolMessage(data: Uint8Array): ProtocolMessage {
    return data as ProtocolMessage
}

export function readOpcode(message: ProtocolMessage): number {
    if (message.length < 8) {
        throw new Error('Protocol message too short')
    }
    
    return (message[4] | (message[5] << 8) | (message[6] << 16) | (message[7] << 24)) >>> 0
}

export function readOpcodeGroup(message: ProtocolMessage): number {
    return readOpcode(message) & 0xFFFF0000
}

// Reads the one header byte the send path needs, without the allocations of
// readProtocolMessageHeaders; a message too short to have it has no flags
export function readFlags(message: ProtocolMessage | Uint8Array): number {
    return message.length >= MinimumHeaderLength ? message[26] : 0
}

function readUInt32Le(bytes: Uint8Array, offset: number): number {
  return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)) >>> 0;
}

// Runs the same checks as readProtocolMessageHeaders without building the headers, and returns the
// header's size
function checkProtocolHeader(bytes: Uint8Array): number {
  if (bytes.length < MinimumHeaderLength) {
    throw new Error('Protocol payload too short');
  }

  const length = readUInt32Le(bytes, 0);

  if (length > bytes.length) {
    throw new Error(`Protocol message declares ${length} bytes but carries ${bytes.length}`);
  }

  const headerSize = MinimumHeaderLength + readUInt32Le(bytes, 20) * 4;

  if (headerSize > bytes.length) {
    throw new Error('Protocol header exceeds payload length');
  }

  return headerSize;
}

export function readProtocolMessageHeaders(raw: ProtocolMessage | ArrayBuffer | Uint8Array): ProtocolMessageHeaders {
  const bytes = toUint8Array(raw);
  const headerSize = checkProtocolHeader(bytes);

  const length = readUInt32Le(bytes, 0);
  const opcode = readUInt32Le(bytes, 4);
  const senderId = readUInt32Le(bytes, 8);
  const trackId = readUInt32Le(bytes, 12);
  const sequenceId = readUInt32Le(bytes, 16);
  const payloadVersion = bytes[24];
  const payloadType = bytes[25];
  const flags = bytes[26];

  const targetIds: number[] = [];

  for (let offset = MinimumHeaderLength; offset < headerSize; offset += 4) {
    targetIds.push(readUInt32Le(bytes, offset));
  }

  return {
    length,
    opcode,
    senderId,
    trackId,
    sequenceId,
    targetIds,
    payloadVersion,
    payloadType,
    flags,
  };
}

export function readProtocolMessagePayload(
  raw: ProtocolMessage | ArrayBuffer | Uint8Array,
  expectedOpcode?: number,
  _expectedVersion?: number,
): Uint8Array {
  const bytes = toUint8Array(raw);
  const headerSize = checkProtocolHeader(bytes);
  const opcode = readUInt32Le(bytes, 4);

  if (expectedOpcode !== undefined && opcode !== expectedOpcode) {
    throw new Error(`Unexpected opcode ${opcode}`);
  }

  // As teleport is our main payload now, the message version does not really need enforcing.
  // if (expectedVersion !== undefined && headers.payloadVersion !== expectedVersion) {
  //   throw new Error(`Unexpected payload version ${headers.payloadVersion}`);
  // }

  if (bytes[25] !== PayloadType.Teleport) {
    throw new Error(`Unexpected payload type ${bytes[25]}`);
  }

  return bytes.subarray(headerSize, readUInt32Le(bytes, 0));
}

export async function readProtocolMessagePayloadAsync(
  raw: ProtocolMessage | ArrayBuffer | Uint8Array,
  expectedOpcode?: number,
  _expectedVersion?: number,
): Promise<Uint8Array> {
  const bytes = toUint8Array(raw);
  const headerSize = checkProtocolHeader(bytes);
  const opcode = readUInt32Le(bytes, 4);

  if (expectedOpcode !== undefined && opcode !== expectedOpcode) {
    throw new Error(`Unexpected opcode ${opcode}`);
  }

  if (bytes[25] !== PayloadType.Teleport) {
    throw new Error(`Unexpected payload type ${bytes[25]}`);
  }

  let payload = bytes.subarray(headerSize, readUInt32Le(bytes, 0));

  if ((bytes[26] & MessageFlag.Compressed) !== 0) {
    payload = await decompressPayloadInternal(payload);
  }

  return payload;
}

export function createProtocolMessage(
  opcode: number,
  payload: Uint8Array,
  payloadVersion: number,
  senderId: number,
  overrides?: ProtocolMessageOverrides,
): ProtocolMessage {
  const trackId = overrides?.trackId ?? 0;
  const sequenceId = overrides?.sequenceId ?? 0;
  const flags = overrides?.flags ?? 0;
  const targetIds = overrides?.targetIds ?? [];
  const payloadType = overrides?.payloadType ?? PayloadType.Teleport;

  return asProtocolMessage(writeProtocolMessage(opcode, payload, payloadVersion, senderId, trackId, sequenceId, flags, targetIds, payloadType));
}

export async function createProtocolMessageAsync(
  opcode: number,
  payload: Uint8Array,
  payloadVersion: number,
  senderId: number,
  overrides?: ProtocolMessageOverrides,
): Promise<ProtocolMessage> {
  const trackId = overrides?.trackId ?? 0;
  const sequenceId = overrides?.sequenceId ?? 0;
  let flags = overrides?.flags ?? 0;
  const targetIds = overrides?.targetIds ?? [];
  const payloadType = overrides?.payloadType ?? PayloadType.Teleport;

  let finalPayload = payload;

  if (overrides?.compress && payload.length >= COMPRESSION_THRESHOLD) {
    try {
      const compressed = await compressPayloadInternal(payload);
      if (compressed !== null) {
        finalPayload = compressed;
        flags |= MessageFlag.Compressed;
      }
    } catch {
      // Compression failed, use uncompressed payload
    }
  }

  return asProtocolMessage(writeProtocolMessage(opcode, finalPayload, payloadVersion, senderId, trackId, sequenceId, flags, targetIds, payloadType));
}

function writeProtocolMessage(opcode: number, payload: Uint8Array, payloadVersion: number, senderId: number, trackId: number, sequenceId: number, flags: number, targetIds: readonly number[], payloadType: number): Uint8Array {
  const headerSize = MinimumHeaderLength + targetIds.length * 4;
  const totalSize = headerSize + payload.length;
  const buffer = new Uint8Array(totalSize);

  writeUInt32Le(buffer, 0, totalSize);
  writeUInt32Le(buffer, 4, opcode);
  writeUInt32Le(buffer, 8, senderId);
  writeUInt32Le(buffer, 12, trackId);
  writeUInt32Le(buffer, 16, sequenceId);
  writeUInt32Le(buffer, 20, targetIds.length);
  buffer[24] = payloadVersion;
  buffer[25] = payloadType;
  buffer[26] = flags;

  for (let i = 0; i < targetIds.length; i++) {
    writeUInt32Le(buffer, MinimumHeaderLength + i * 4, targetIds[i]);
  }

  buffer.set(payload, headerSize);
  return buffer;
}

function writeUInt32Le(bytes: Uint8Array, offset: number, value: number): void {
  bytes[offset] = value;
  bytes[offset + 1] = value >>> 8;
  bytes[offset + 2] = value >>> 16;
  bytes[offset + 3] = value >>> 24;
}

// Compression threshold in bytes
const COMPRESSION_THRESHOLD = 1024;

async function compressPayloadInternal(data: Uint8Array): Promise<Uint8Array | null> {
  if (typeof CompressionStream === 'undefined') {
    return null;
  }

  const stream = new CompressionStream('gzip');
  const writer = stream.writable.getWriter();

  // Copy to ensure we have a standard ArrayBuffer, not SharedArrayBuffer
  const copy = new Uint8Array(data);
  // Deliberately not awaited before the read loop: with a compression stream the write only settles
  // once the readable side is drained, so awaiting here would deadlock on any payload larger than
  // the internal queue. The reader below reports a compression failure; this catch exists only so
  // the writer's copy of that same error does not surface as an unhandled rejection.
  void writer
    .write(copy)
    .then(() => writer.close())
    .catch(() => undefined);

  const reader = stream.readable.getReader();
  const chunks: Uint8Array[] = [];
  let totalLength = 0;

  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
    totalLength += value.length;
  }

  // Don't compress if it made data larger
  if (totalLength >= data.length) {
    return null;
  }

  const result = new Uint8Array(totalLength);
  let offset = 0;
  for (let i = 0; i < chunks.length; i++) {
    result.set(chunks[i], offset);
    offset += chunks[i].length;
  }

  return result;
}

async function decompressPayloadInternal(data: Uint8Array): Promise<Uint8Array> {
  if (typeof DecompressionStream === 'undefined') {
    throw new Error('DecompressionStream not supported');
  }

  const stream = new DecompressionStream('gzip');
  const writer = stream.writable.getWriter();
  // Copy to ensure we have a standard ArrayBuffer, not SharedArrayBuffer
  const copy = new Uint8Array(data);
  // Deliberately not awaited before the read loop: with a compression stream the write only settles
  // once the readable side is drained, so awaiting here would deadlock on any payload larger than
  // the internal queue. The reader below reports a compression failure; this catch exists only so
  // the writer's copy of that same error does not surface as an unhandled rejection.
  void writer
    .write(copy)
    .then(() => writer.close())
    .catch(() => undefined);

  const reader = stream.readable.getReader();
  const chunks: Uint8Array[] = [];
  let totalLength = 0;

  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
    totalLength += value.length;

    // A few kilobytes of gzip can expand without limit, so the ceiling is checked as it grows rather
    // than on the finished buffer the tab would already have died building.
    if (totalLength > MaxMessageSize) {
      await reader.cancel().catch(() => undefined); // The error below is what the caller acts on; a failed cancel changes nothing
      throw new Error(`Decompressed payload exceeds the maximum of ${MaxMessageSize} bytes`);
    }
  }

  const result = new Uint8Array(totalLength);
  let offset = 0;
  for (let i = 0; i < chunks.length; i++) {
    result.set(chunks[i], offset);
    offset += chunks[i].length;
  }

  return result;
}

export enum TeleportType {
  Null = 0x01,
  Bool = 0x02,
  Int32 = 0x03,
  Int64 = 0x04,
  UInt32 = 0x05,
  UInt64 = 0x06,
  Float32 = 0x07,
  Float64 = 0x08,
  Array = 0x09,
  Dict = 0x0a,
  Object = 0x0b,
  String = 0x0c,
  Binary = 0x0d,
  Guid = 0x0e,
}

const OBJECT_START = 0xa1;
const OBJECT_END = 0xa2;
const utf8Encoder = new TextEncoder();
const utf8Decoder = new TextDecoder('utf-8', { fatal: true });

// Strings up to this many UTF-16 units are tried as ASCII with a plain loop, which is faster than
// calling the encoder. Anything longer, or not ASCII, goes through encodeInto.
const ASCII_FAST_PATH_MAX = 64;
// Longer strings go through encode(), which sizes the result exactly, because encodeInto would need
// room for three bytes per unit.
const ENCODE_INTO_MAX = 65536;

/**
 * The one buffer a message is written into. Nested objects, arrays and dictionaries are written in
 * place: the length or count in front of each one gets one byte up front and is widened when it is
 * written, so no container is built in a buffer of its own and copied into its parent.
 *
 * `owner` is the writer allowed to write next. A nested writer takes it over while its build runs
 * and hands it back when it ends, so a write through a scope that has ended, or through a parent
 * while a child is open, is refused rather than interleaved into the bytes.
 */
class ByteBuffer {
  public bytes: Uint8Array;
  public view: DataView;
  public length = 0;
  public owner: object | null = null;

  constructor(capacity = 256) {
    this.bytes = new Uint8Array(capacity);
    this.view = new DataView(this.bytes.buffer);
  }

  public claim(writer: object): void {
    if (this.owner !== writer) {
      throw new Error('Teleport writer used outside its scope');
    }
  }

  public ensureCapacity(additional: number): void {
    const required = this.length + additional;
    if (required <= this.bytes.length) {
      return;
    }

    let capacity = this.bytes.length * 2;
    while (capacity < required) {
      capacity *= 2;
    }

    const bytes = new Uint8Array(capacity);
    bytes.set(this.bytes.subarray(0, this.length));
    this.bytes = bytes;
    this.view = new DataView(bytes.buffer);
  }

  public writeByte(value: number): void {
    this.ensureCapacity(1);
    this.bytes[this.length++] = value & 0xff;
  }

  public writeBytes(value: Uint8Array): void {
    this.ensureCapacity(value.length);
    this.bytes.set(value, this.length);
    this.length += value.length;
  }

  public writeUInt32(value: number): void {
    this.ensureCapacity(4);
    const bytes = this.bytes;
    const at = this.length;
    bytes[at] = value;
    bytes[at + 1] = value >>> 8;
    bytes[at + 2] = value >>> 16;
    bytes[at + 3] = value >>> 24;
    this.length = at + 4;
  }

  public writeInt32(value: number): void {
    this.writeUInt32(value | 0);
  }

  public writeFloat32(value: number): void {
    this.ensureCapacity(4);
    this.view.setFloat32(this.length, value, true);
    this.length += 4;
  }

  public writeFloat64(value: number): void {
    this.ensureCapacity(8);
    this.view.setFloat64(this.length, value, true);
    this.length += 8;
  }

  public writeBigInt64(value: bigint): void {
    this.ensureCapacity(8);
    this.view.setBigInt64(this.length, value, true);
    this.length += 8;
  }

  public writeBigUInt64(value: bigint): void {
    this.ensureCapacity(8);
    this.view.setBigUint64(this.length, value, true);
    this.length += 8;
  }

  public writeVarUInt(value: number): void {
    this.ensureCapacity(5);
    this.length = writeVarUIntAt(this.bytes, this.length, value >>> 0);
  }

  /** A length-prefixed UTF-8 string, encoded straight into the buffer. */
  public writeString(input: string): void {
    // Untyped callers pass numbers and other non-strings, and the earlier encoder wrote their String()
    // form, so this does too
    const value = typeof input === 'string' ? input : input === undefined || input === null ? '' : String(input);
    const units = value.length;

    if (units <= ASCII_FAST_PATH_MAX) {
      this.ensureCapacity(1 + units);
      const bytes = this.bytes;
      const start = this.length + 1;
      let i = 0;

      for (; i < units; i++) {
        const unit = value.charCodeAt(i);
        if (unit >= 0x80) {
          break;
        }

        bytes[start + i] = unit;
      }

      if (i === units) {
        bytes[this.length] = units;
        this.length = start + units;
        return;
      }
    }

    if (units > ENCODE_INTO_MAX) {
      const encoded = utf8Encoder.encode(value);
      this.writeVarUInt(encoded.length);
      this.writeBytes(encoded);
      return;
    }

    const maxBytes = units * 3;
    const reserved = getVarUIntLength(maxBytes);
    this.ensureCapacity(reserved + maxBytes);

    const at = this.length;
    const written = utf8Encoder.encodeInto(value, this.bytes.subarray(at + reserved, at + reserved + maxBytes)).written;
    const prefix = getVarUIntLength(written);

    if (prefix < reserved) {
      this.bytes.copyWithin(at + prefix, at + reserved, at + reserved + written);
    }

    writeVarUIntAt(this.bytes, at, written);
    this.length = at + prefix + written;
  }

  /** Reserves one byte for a varuint that {@link patchVarUInt} writes later, and returns its offset. */
  public reserveVarUInt(): number {
    this.ensureCapacity(1);
    return this.length++;
  }

  /** Writes `value` at `at`, moving everything after it when it needs more than the reserved byte. */
  public patchVarUInt(at: number, value: number): void {
    const size = getVarUIntLength(value);

    if (size > 1) {
      this.ensureCapacity(size - 1);
      this.bytes.copyWithin(at + size, at + 1, this.length);
      this.length += size - 1;
    }

    writeVarUIntAt(this.bytes, at, value);
  }

  /** Fills a reserved length with the number of bytes written after it. */
  public patchLength(at: number): void {
    this.patchVarUInt(at, this.length - at - 1);
  }

  public toUint8Array(): Uint8Array {
    return this.bytes.slice(0, this.length);
  }
}

export class TeleportObjectWriter {
  private readonly out: ByteBuffer;
  private readonly nested: boolean;
  private cached?: Uint8Array;

  /** @param out - The buffer of the writer this object is nested in; only the runtime passes it. */
  public constructor(version = 1, out?: ByteBuffer) {
    this.nested = out !== undefined;
    this.out = out ?? new ByteBuffer();
    this.out.owner = this;
    this.out.writeByte(OBJECT_START);
    this.out.writeVarUInt(version >>> 0);
  }

  public writeInt32Field(fieldId: number, value: number): void {
    this.writeFieldHeader(fieldId, TeleportType.Int32);
    this.out.writeInt32(value);
  }

  public writeUInt32Field(fieldId: number, value: number): void {
    this.writeFieldHeader(fieldId, TeleportType.UInt32);
    this.out.writeUInt32(value >>> 0);
  }

  public writeInt64Field(fieldId: number, value: bigint): void {
    this.writeFieldHeader(fieldId, TeleportType.Int64);
    this.out.writeBigInt64(value);
  }

  public writeUInt64Field(fieldId: number, value: bigint): void {
    this.writeFieldHeader(fieldId, TeleportType.UInt64);
    this.out.writeBigUInt64(value);
  }

  public writeFloat32Field(fieldId: number, value: number): void {
    this.writeFieldHeader(fieldId, TeleportType.Float32);
    this.out.writeFloat32(value);
  }

  public writeFloat64Field(fieldId: number, value: number): void {
    this.writeFieldHeader(fieldId, TeleportType.Float64);
    this.out.writeFloat64(value);
  }

  public writeBoolField(fieldId: number, value: boolean): void {
    this.writeFieldHeader(fieldId, TeleportType.Bool);
    this.out.writeByte(value ? 1 : 0);
  }

  public writeGuidField(fieldId: number, guid: TeleportGuid | Uint8Array): void {
    const bytes = guidBytes(guid);
    this.writeFieldHeader(fieldId, TeleportType.Guid);
    this.out.writeBytes(bytes);
  }

  public writeStringField(fieldId: number, value: string): void {
    this.writeFieldHeader(fieldId, TeleportType.String);
    this.out.writeString(value ?? '');
  }

  public writeBinaryField(fieldId: number, value: Uint8Array): void {
    this.writeFieldHeader(fieldId, TeleportType.Binary);
    this.out.writeVarUInt(value.length);
    this.out.writeBytes(value);
  }

  public writeObjectField(fieldId: number, version: number, build: (scope: TeleportObjectWriter) => void): void {
    this.writeFieldHeader(fieldId, TeleportType.Object);
    const out = this.out;
    const length = out.reserveVarUInt();
    const nested = new TeleportObjectWriter(version, out);
    build(nested);
    nested.close();
    out.owner = this;
    out.patchLength(length);
  }

  public writeArrayField(fieldId: number, elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    this.writeFieldHeader(fieldId, TeleportType.Array);
    const out = this.out;
    const length = out.reserveVarUInt();
    const nested = new TeleportArrayWriter(elementType, out);
    build(nested);
    nested.close();
    out.owner = this;
    out.patchLength(length);
  }

  public writeDictionaryField(
    fieldId: number,
    keyType: TeleportType,
    valueType: TeleportType,
    build: (scope: TeleportDictWriter) => void,
  ): void {
    this.writeFieldHeader(fieldId, TeleportType.Dict);
    const out = this.out;
    const length = out.reserveVarUInt();
    const nested = new TeleportDictWriter(keyType, valueType, out);
    build(nested);
    nested.close();
    out.owner = this;
    out.patchLength(length);
  }

  /** The encoded object. Only the outermost writer is finished; a nested one ends when its build returns. */
  public finish(): Uint8Array {
    if (this.nested) {
      throw new Error('A nested Teleport object is finished by its parent');
    }

    // The cached buffer is the closed signal: once it exists the object has been finished.
    if (!this.cached) {
      this.close();
      this.cached = this.out.toUint8Array();
    }

    return this.cached;
  }

  /** @internal Ends the object. The parent then takes back ownership of the buffer. */
  public close(): void {
    this.out.claim(this);
    this.out.writeByte(OBJECT_END);
    this.out.owner = null;
  }

  private writeFieldHeader(fieldId: number, type: TeleportType): void {
    const out = this.out;
    out.claim(this);
    out.ensureCapacity(5);
    const bytes = out.bytes;
    const at = out.length;
    bytes[at] = fieldId;
    bytes[at + 1] = fieldId >>> 8;
    bytes[at + 2] = fieldId >>> 16;
    bytes[at + 3] = fieldId >>> 24;
    bytes[at + 4] = type << 4;
    out.length = at + 5;
  }
}

export class TeleportArrayWriter {
  private readonly out: ByteBuffer;
  private readonly countAt: number;
  private readonly nested: boolean;
  private count = 0;
  private cached?: Uint8Array;

  /** @param out - The buffer of the writer this array is nested in; only the runtime passes it. */
  public constructor(private readonly elementType: TeleportType, out?: ByteBuffer) {
    this.nested = out !== undefined;
    this.out = out ?? new ByteBuffer();
    this.out.owner = this;
    this.out.writeByte(composeDescriptor(elementType));
    this.countAt = this.out.reserveVarUInt();
  }

  public writeInt32(value: number): void {
    this.begin(TeleportType.Int32);
    this.out.writeInt32(value);
  }

  public writeUInt32(value: number): void {
    this.begin(TeleportType.UInt32);
    this.out.writeUInt32(value >>> 0);
  }

  public writeInt64(value: bigint): void {
    this.begin(TeleportType.Int64);
    this.out.writeBigInt64(value);
  }

  public writeUInt64(value: bigint): void {
    this.begin(TeleportType.UInt64);
    this.out.writeBigUInt64(value);
  }

  public writeFloat32(value: number): void {
    this.begin(TeleportType.Float32);
    this.out.writeFloat32(value);
  }

  public writeFloat64(value: number): void {
    this.begin(TeleportType.Float64);
    this.out.writeFloat64(value);
  }

  public writeBool(value: boolean): void {
    this.begin(TeleportType.Bool);
    this.out.writeByte(value ? 1 : 0);
  }

  public writeGuid(value: TeleportGuid | Uint8Array): void {
    const bytes = guidBytes(value);
    this.begin(TeleportType.Guid);
    this.out.writeBytes(bytes);
  }

  public writeString(value: string): void {
    this.begin(TeleportType.String);
    this.out.writeString(value ?? '');
  }

  public writeBinary(value: Uint8Array): void {
    this.begin(TeleportType.Binary);
    this.out.writeVarUInt(value.length);
    this.out.writeBytes(value);
  }

  public writeObject(version: number, build: (scope: TeleportObjectWriter) => void): void {
    this.begin(TeleportType.Object);
    const out = this.out;
    const length = out.reserveVarUInt();
    const nested = new TeleportObjectWriter(version, out);
    build(nested);
    nested.close();
    out.owner = this;
    out.patchLength(length);
  }

  public writeArray(elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    this.begin(TeleportType.Array);
    const nested = new TeleportArrayWriter(elementType, this.out);
    build(nested);
    nested.close();
    this.out.owner = this;
  }

  public writeDictionary(keyType: TeleportType, valueType: TeleportType, build: (scope: TeleportDictWriter) => void): void {
    this.begin(TeleportType.Dict);
    const nested = new TeleportDictWriter(keyType, valueType, this.out);
    build(nested);
    nested.close();
    this.out.owner = this;
  }

  /** The encoded array, for an array written on its own rather than as a field or element. */
  public finish(): Uint8Array {
    if (this.nested) {
      throw new Error('A nested Teleport container is finished by its parent');
    }

    if (!this.cached) {
      this.close();
      this.cached = this.out.toUint8Array();
    }

    return this.cached;
  }

  /** @internal Writes the element count. The parent then takes back ownership of the buffer. */
  public close(): void {
    this.out.claim(this);
    this.out.patchVarUInt(this.countAt, this.count);
    this.out.owner = null;
  }

  private begin(expected: TeleportType): void {
    if (this.elementType !== expected) {
      throw new Error(`Array element type is ${TeleportType[this.elementType]}, expected ${TeleportType[expected]}`);
    }

    this.out.claim(this);
    this.count++;
  }
}

export class TeleportDictWriter {
  private readonly out: ByteBuffer;
  private readonly countAt: number;
  private readonly nested: boolean;
  private count = 0;
  private entryOpen = false;
  private cached?: Uint8Array;

  /** @param out - The buffer of the writer this dictionary is nested in; only the runtime passes it. */
  public constructor(private readonly keyType: TeleportType, private readonly valueType: TeleportType, out?: ByteBuffer) {
    ensurePrimitiveKey(keyType);
    this.nested = out !== undefined;
    this.out = out ?? new ByteBuffer();
    this.out.owner = this;
    this.out.writeByte(composeDescriptor(keyType));
    this.out.writeByte(composeDescriptor(valueType));
    this.countAt = this.out.reserveVarUInt();
  }

  public beginEntry(): TeleportDictEntryWriter {
    if (this.entryOpen) {
      throw new Error('Previous dictionary entry not completed');
    }

    this.out.claim(this);
    this.count++;
    this.entryOpen = true;
    return new TeleportDictEntryWriter(this.keyType, this.valueType, this.out, () => {
      this.entryOpen = false;
      this.out.owner = this;
    });
  }

  /** The encoded dictionary, for one written on its own rather than as a field or element. */
  public finish(): Uint8Array {
    if (this.nested) {
      throw new Error('A nested Teleport container is finished by its parent');
    }

    if (!this.cached) {
      this.close();
      this.cached = this.out.toUint8Array();
    }

    return this.cached;
  }

  /** @internal Writes the entry count. The parent then takes back ownership of the buffer. */
  public close(): void {
    if (this.entryOpen) {
      throw new Error('Dictionary entry not completed');
    }

    this.out.claim(this);
    this.out.patchVarUInt(this.countAt, this.count);
    this.out.owner = null;
  }
}

export class TeleportDictEntryWriter {
  private keyWritten = false;
  private valueWritten = false;
  private completed = false;

  public constructor(
    private readonly keyType: TeleportType,
    private readonly valueType: TeleportType,
    private readonly payload: ByteBuffer,
    private readonly onComplete: () => void,
  ) {
    payload.owner = this;
  }

  public writeKeyInt32(value: number): void {
    this.beginKey(TeleportType.Int32);
    this.payload.writeInt32(value);
  }

  public writeKeyUInt32(value: number): void {
    this.beginKey(TeleportType.UInt32);
    this.payload.writeUInt32(value >>> 0);
  }

  public writeKeyInt64(value: bigint): void {
    this.beginKey(TeleportType.Int64);
    this.payload.writeBigInt64(value);
  }

  public writeKeyUInt64(value: bigint): void {
    this.beginKey(TeleportType.UInt64);
    this.payload.writeBigUInt64(value);
  }

  public writeKeyFloat32(value: number): void {
    this.beginKey(TeleportType.Float32);
    this.payload.writeFloat32(value);
  }

  public writeKeyFloat64(value: number): void {
    this.beginKey(TeleportType.Float64);
    this.payload.writeFloat64(value);
  }

  public writeKeyBool(value: boolean): void {
    this.beginKey(TeleportType.Bool);
    this.payload.writeByte(value ? 1 : 0);
  }

  public writeKeyGuid(value: TeleportGuid | Uint8Array): void {
    const bytes = guidBytes(value);
    this.beginKey(TeleportType.Guid);
    this.payload.writeBytes(bytes);
  }

  public writeKeyString(value: string): void {
    this.beginKey(TeleportType.String);
    this.payload.writeString(value ?? '');
  }

  public writeKeyBinary(value: Uint8Array): void {
    this.beginKey(TeleportType.Binary);
    this.payload.writeVarUInt(value.length);
    this.payload.writeBytes(value);
  }

  public writeValueInt32(value: number): void {
    this.beginValue(TeleportType.Int32);
    this.payload.writeInt32(value);
  }

  public writeValueUInt32(value: number): void {
    this.beginValue(TeleportType.UInt32);
    this.payload.writeUInt32(value >>> 0);
  }

  public writeValueInt64(value: bigint): void {
    this.beginValue(TeleportType.Int64);
    this.payload.writeBigInt64(value);
  }

  public writeValueUInt64(value: bigint): void {
    this.beginValue(TeleportType.UInt64);
    this.payload.writeBigUInt64(value);
  }

  public writeValueFloat32(value: number): void {
    this.beginValue(TeleportType.Float32);
    this.payload.writeFloat32(value);
  }

  public writeValueFloat64(value: number): void {
    this.beginValue(TeleportType.Float64);
    this.payload.writeFloat64(value);
  }

  public writeValueBool(value: boolean): void {
    this.beginValue(TeleportType.Bool);
    this.payload.writeByte(value ? 1 : 0);
  }

  public writeValueGuid(value: TeleportGuid | Uint8Array): void {
    const bytes = guidBytes(value);
    this.beginValue(TeleportType.Guid);
    this.payload.writeBytes(bytes);
  }

  public writeNullValue(): void {
    this.beginValue(TeleportType.Null);
  }

  public writeValueBinary(value: Uint8Array): void {
    this.beginValue(TeleportType.Binary);
    this.payload.writeVarUInt(value.length);
    this.payload.writeBytes(value);
  }

  public writeValueString(value: string): void {
    this.beginValue(TeleportType.String);
    this.payload.writeString(value ?? '');
  }

  public writeValueObject(version: number, build: (scope: TeleportObjectWriter) => void): void {
    this.beginValue(TeleportType.Object);
    const out = this.payload;
    const length = out.reserveVarUInt();
    const nested = new TeleportObjectWriter(version, out);
    build(nested);
    nested.close();
    out.owner = this;
    out.patchLength(length);
  }

  public writeValueArray(elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    this.beginValue(TeleportType.Array);
    const nested = new TeleportArrayWriter(elementType, this.payload);
    build(nested);
    nested.close();
    this.payload.owner = this;
  }

  public writeValueDictionary(keyType: TeleportType, valueType: TeleportType, build: (scope: TeleportDictWriter) => void): void {
    this.beginValue(TeleportType.Dict);
    const nested = new TeleportDictWriter(keyType, valueType, this.payload);
    build(nested);
    nested.close();
    this.payload.owner = this;
  }

  public complete(): void {
    if (this.completed) {
      return;
    }

    if (!this.keyWritten || !this.valueWritten) {
      throw new Error('Dictionary entry must write both key and value');
    }

    this.completed = true;
    this.onComplete();
  }

  // The key comes before the value on the wire, so writing the value first, or a second key, would put
  // bytes in the wrong place
  private beginKey(expected: TeleportType): void {
    if (this.keyType !== expected) {
      throw new Error(`Dictionary key type is ${TeleportType[this.keyType]}, expected ${TeleportType[expected]}`);
    }

    if (this.keyWritten) {
      throw new Error('Dictionary entry key already written');
    }

    this.payload.claim(this);
    this.keyWritten = true;
  }

  private beginValue(expected: TeleportType): void {
    if (this.valueType !== expected) {
      throw new Error(`Dictionary value type is ${TeleportType[this.valueType]}, expected ${TeleportType[expected]}`);
    }

    if (!this.keyWritten || this.valueWritten) {
      throw new Error(this.keyWritten ? 'Dictionary entry value already written' : 'Dictionary entry key must be written before its value');
    }

    this.payload.claim(this);
    this.valueWritten = true;
  }
}

export class TeleportObjectReader {
  private offset: number;
  private readonly end: number;
  private field: TeleportField | undefined;

  private constructor(private readonly buffer: Uint8Array, private readonly view: DataView, public readonly version: number, startOffset: number, end: number) {
    this.offset = startOffset;
    this.end = end;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportObjectReader {
    const bytes = toUint8Array(data);
    return TeleportObjectReader.open(bytes, new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), 0, bytes.length);
  }

  /** @internal Reads the `size`-byte object at `start` without copying it out of `bytes`. */
  public static open(bytes: Uint8Array, view: DataView, start: number, size: number): TeleportObjectReader {
    if (size < 2) {
      throw new Error('Teleport payload too short');
    }

    const last = start + size - 1;

    if (bytes[start] !== OBJECT_START || bytes[last] !== OBJECT_END) {
      throw new Error('Teleport object missing markers');
    }

    const version = readVarUInt(bytes, start + 1, last, 'InvalidLength');
    return new TeleportObjectReader(bytes, view, version, varUIntEnd, last);
  }

  /** The next field, or null after the last. Every call returns the same object, updated to the next field, so read a field before calling `next` again. */
  public next(): TeleportField | null {
    const buffer = this.buffer;
    const end = this.end;
    let offset = this.offset;

    if (offset >= end) {
      return null;
    }

    if (offset + 5 > end) {
      throw new Error('Teleport object truncated');
    }

    const fieldId = (buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24)) >>> 0;
    const descriptor = buffer[offset + 4];
    offset += 5;

    if ((descriptor & 0x0f) !== 0) {
      throw new Error('Teleport field flags must be zero');
    }

    const type = descriptor >> 4;
    let size = getFixedSize(type);

    if (size < 0) {
      size = readVarUInt(buffer, offset, end, 'InvalidLength');
      offset = varUIntEnd;
    }

    // The end marker is not part of any field, so a field running into it is malformed
    if (offset + size > end) {
      throw new Error('Teleport payload exceeds bounds');
    }

    this.offset = offset + size;
    const field = this.field;

    if (field === undefined) {
      return (this.field = new TeleportField(fieldId, type, buffer, offset, size, this.view));
    }

    field.fieldId = fieldId;
    field.moveTo(type, offset, size);
    return field;
  }
}

export class TeleportValue {
  public type: TeleportType;
  protected readonly bytes: Uint8Array;
  protected start: number;
  protected size: number;
  private view: DataView | undefined;

  /**
   * @param payload - The value's bytes, or the buffer they sit in when `start` and `size` are given.
   * @param view - A view over all of `payload`, shared by the values read from one buffer.
   */
  public constructor(type: TeleportType, payload: Uint8Array, start = 0, size = payload.length - start, view?: DataView) {
    this.type = type;
    this.bytes = payload;
    this.start = start;
    this.size = size;
    this.view = view;
  }

  /** @internal Points this value at the next one in the same buffer. */
  public moveTo(type: TeleportType, start: number, size: number): void {
    this.type = type;
    this.start = start;
    this.size = size;
  }

  public asInt32(): number {
    this.ensureType(TeleportType.Int32);
    const b = this.bytes;
    const i = this.start;
    return b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24);
  }

  public asUInt32(): number {
    this.ensureType(TeleportType.UInt32);
    const b = this.bytes;
    const i = this.start;
    return (b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24)) >>> 0;
  }

  public asInt64(): bigint {
    this.ensureType(TeleportType.Int64);
    return this.dataView().getBigInt64(this.start, true);
  }

  public asUInt64(): bigint {
    this.ensureType(TeleportType.UInt64);
    return this.dataView().getBigUint64(this.start, true);
  }

  public asFloat32(): number {
    this.ensureType(TeleportType.Float32);
    return this.dataView().getFloat32(this.start, true);
  }

  public asFloat64(): number {
    this.ensureType(TeleportType.Float64);
    return this.dataView().getFloat64(this.start, true);
  }

  public asBool(): boolean {
    this.ensureType(TeleportType.Bool);
    return this.size > 0 && this.bytes[this.start] !== 0;
  }

  /** A view into the received bytes, not a copy. */
  public asBinary(): Uint8Array {
    this.ensureType(TeleportType.Binary);
    return this.bytes.subarray(this.start, this.start + this.size);
  }

  public asUtf8(): Uint8Array {
    this.ensureType(TeleportType.String);
    return this.bytes.subarray(this.start, this.start + this.size);
  }

  public asString(): string {
    this.ensureType(TeleportType.String);
    return decodeUtf8(this.bytes, this.start, this.size);
  }

  public asGuid(): TeleportGuid {
    this.ensureType(TeleportType.Guid);
    return TeleportGuid.fromBytes(this.bytes.subarray(this.start, this.start + this.size));
  }

  public asObject(): TeleportObjectReader {
    this.ensureType(TeleportType.Object);
    return TeleportObjectReader.open(this.bytes, this.dataView(), this.start, this.size);
  }

  public asArray(): TeleportArrayReader {
    this.ensureType(TeleportType.Array);
    return TeleportArrayReader.open(this.bytes, this.dataView(), this.start, this.size);
  }

  public asDictionary(): TeleportDictReader {
    this.ensureType(TeleportType.Dict);
    return TeleportDictReader.open(this.bytes, this.dataView(), this.start, this.size);
  }

  protected ensureType(expected: TeleportType): void {
    if (this.type !== expected) {
      throw new Error(`Teleport value has type ${TeleportType[this.type]}, expected ${TeleportType[expected]}`);
    }
  }

  private dataView(): DataView {
    this.view ??= new DataView(this.bytes.buffer, this.bytes.byteOffset, this.bytes.byteLength);
    return this.view;
  }
}

export class TeleportField extends TeleportValue {
  public fieldId: number;

  public constructor(fieldId: number, type: TeleportType, payload: Uint8Array, start?: number, size?: number, view?: DataView) {
    super(type, payload, start, size, view);
    this.fieldId = fieldId;
  }

  public get isNull(): boolean {
    return this.type === TeleportType.Null;
  }
}

export class TeleportArrayReader {
  private readonly elementType: TeleportType;
  private readonly count: number;
  private offset: number;
  private index = 0;
  private element: TeleportArrayElement | undefined;

  private constructor(private readonly payload: Uint8Array, private readonly view: DataView, start: number, private readonly end: number) {
    if (end <= start) {
      throw new Error('Array payload too short');
    }

    const descriptor = payload[start];
    this.elementType = descriptor >> 4;

    if ((descriptor & 0x0f) !== 0) {
      throw new Error('Array flags must be zero');
    }

    this.count = readVarUInt(payload, start + 1, end, 'ArrayMalformed');
    this.offset = varUIntEnd;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportArrayReader {
    const bytes = toUint8Array(data);
    return new TeleportArrayReader(bytes, new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), 0, bytes.length);
  }

  /** @internal Reads the `size`-byte array at `start` without copying it out of `bytes`. */
  public static open(bytes: Uint8Array, view: DataView, start: number, size: number): TeleportArrayReader {
    return new TeleportArrayReader(bytes, view, start, start + size);
  }

  /** The next element, or null after the last. Every call returns the same object, updated to the next element. */
  public next(): TeleportArrayElement | null {
    if (this.index >= this.count) {
      if (this.offset !== this.end) {
        throw new Error('Array payload contains trailing data');
      }

      return null;
    }

    const type = this.elementType;
    const start = readInlineValue(type, this.payload, this.offset, this.end, 'ArrayMalformed', 0);
    this.offset = inlineValueEnd;
    this.index++;
    const element = this.element;

    if (element === undefined) {
      return (this.element = new TeleportArrayElement(type, this.payload, start, inlineValueEnd - start, this.view));
    }

    element.moveTo(type, start, inlineValueEnd - start);
    return element;
  }
}

export class TeleportArrayElement extends TeleportValue {
  public constructor(type: TeleportType, payload: Uint8Array, start?: number, size?: number, view?: DataView) {
    super(type, payload, start, size, view);
  }
}

export class TeleportDictReader {
  private readonly keyType: TeleportType;
  private readonly valueType: TeleportType;
  private readonly count: number;
  private offset: number;
  private index = 0;
  private entry: TeleportDictEntry | undefined;

  private constructor(private readonly payload: Uint8Array, private readonly view: DataView, start: number, private readonly end: number) {
    if (end - start < 2) {
      throw new Error('Dictionary payload too short');
    }

    this.keyType = payload[start] >> 4;
    this.valueType = payload[start + 1] >> 4;

    if ((payload[start] & 0x0f) !== 0 || (payload[start + 1] & 0x0f) !== 0) {
      throw new Error('Dictionary key/value flags must be zero');
    }

    ensurePrimitiveKey(this.keyType);
    this.count = readVarUInt(payload, start + 2, end, 'DictMalformed');
    this.offset = varUIntEnd;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportDictReader {
    const bytes = toUint8Array(data);
    return new TeleportDictReader(bytes, new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), 0, bytes.length);
  }

  /** @internal Reads the `size`-byte dictionary at `start` without copying it out of `bytes`. */
  public static open(bytes: Uint8Array, view: DataView, start: number, size: number): TeleportDictReader {
    return new TeleportDictReader(bytes, view, start, start + size);
  }

  /** The next entry, or null after the last. Every call returns the same entry, key and value objects, updated to the next entry. */
  public next(): TeleportDictEntry | null {
    if (this.index >= this.count) {
      if (this.offset !== this.end) {
        throw new Error('Dictionary payload contains trailing data');
      }

      return null;
    }

    const keyStart = readInlineValue(this.keyType, this.payload, this.offset, this.end, 'DictMalformed', 0);
    const keyEnd = inlineValueEnd;
    const valueStart = readInlineValue(this.valueType, this.payload, keyEnd, this.end, 'DictMalformed', 0);
    const valueEnd = inlineValueEnd;
    this.offset = valueEnd;
    this.index++;
    const entry = this.entry;

    if (entry === undefined) {
      return (this.entry = new TeleportDictEntry(
        new TeleportValue(this.keyType, this.payload, keyStart, keyEnd - keyStart, this.view),
        new TeleportValue(this.valueType, this.payload, valueStart, valueEnd - valueStart, this.view),
      ));
    }

    entry.key.moveTo(this.keyType, keyStart, keyEnd - keyStart);
    entry.value.moveTo(this.valueType, valueStart, valueEnd - valueStart);
    return entry;
  }
}

export class TeleportDictEntry {
  public constructor(public readonly key: TeleportValue, public readonly value: TeleportValue) {}
}

export class TeleportGuid {
  private constructor(private readonly bytes: Uint8Array) {}

  public static fromString(value: string): TeleportGuid {
    if (!value) {
      throw new Error('Guid string is empty');
    }

    const normalized = value.replace(/-/g, '');
    if (normalized.length !== 32) {
      throw new Error('Guid string must be 32 hex characters');
    }

    const bytes = new Uint8Array(16);
    const data1 = TeleportGuid.parseHexSlice(normalized, 0, 8);
    const data2 = TeleportGuid.parseHexSlice(normalized, 8, 4);
    const data3 = TeleportGuid.parseHexSlice(normalized, 12, 4);

    TeleportGuid.writeUInt32LE(bytes, 0, data1);
    TeleportGuid.writeUInt16LE(bytes, 4, data2);
    TeleportGuid.writeUInt16LE(bytes, 6, data3);

    for (let i = 0; i < 8; i++) {
      bytes[8 + i] = TeleportGuid.parseHexSlice(normalized, 16 + i * 2, 2);
    }

    return new TeleportGuid(bytes);
  }

  public static fromBytes(bytes: ArrayLike<number>): TeleportGuid {
    if (bytes.length !== 16) {
      throw new Error('Guid byte array must be 16 bytes');
    }

    return new TeleportGuid(bytes instanceof Uint8Array ? bytes.slice() : Uint8Array.from(bytes));
  }

  public static createZero(): TeleportGuid {
    return new TeleportGuid(new Uint8Array(16));
  }

  public static createRandom(): TeleportGuid {
    const bytes = new Uint8Array(16);
    const cryptoObj = (globalThis as { crypto?: Crypto }).crypto;
    if (cryptoObj?.getRandomValues) {
      cryptoObj.getRandomValues(bytes);
    } else {
      for (let i = 0; i < bytes.length; i++) {
        bytes[i] = Math.floor(Math.random() * 256);
      }
    }

    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;

    return new TeleportGuid(bytes);
  }

  public toString(): string {
    const b = this.bytes;
    const segments = [
      TeleportGuid.toHex(TeleportGuid.readUInt32LE(b, 0), 8),
      TeleportGuid.toHex(TeleportGuid.readUInt16LE(b, 4), 4),
      TeleportGuid.toHex(TeleportGuid.readUInt16LE(b, 6), 4),
      bytesToHex(b.subarray(8, 10)),
      bytesToHex(b.subarray(10, 16)),
    ];

    return segments.join('-');
  }

  public asBytes(): Uint8Array {
    return this.bytes.slice();
  }

  private static parseHexSlice(value: string, start: number, length: number): number {
    const slice = value.substr(start, length);
    const parsed = Number.parseInt(slice, 16);
    if (Number.isNaN(parsed)) {
      throw new Error('Guid string contains invalid characters');
    }

    return parsed >>> 0;
  }

  private static writeUInt32LE(buffer: Uint8Array, offset: number, value: number): void {
    const normalized = value >>> 0;
    buffer[offset] = normalized & 0xff;
    buffer[offset + 1] = (normalized >>> 8) & 0xff;
    buffer[offset + 2] = (normalized >>> 16) & 0xff;
    buffer[offset + 3] = (normalized >>> 24) & 0xff;
  }

  private static writeUInt16LE(buffer: Uint8Array, offset: number, value: number): void {
    const normalized = value & 0xffff;
    buffer[offset] = normalized & 0xff;
    buffer[offset + 1] = (normalized >>> 8) & 0xff;
  }

  private static readUInt32LE(bytes: Uint8Array, offset: number): number {
    return (
      bytes[offset] |
      (bytes[offset + 1] << 8) |
      (bytes[offset + 2] << 16) |
      (bytes[offset + 3] << 24)
    ) >>> 0;
  }

  private static readUInt16LE(bytes: Uint8Array, offset: number): number {
    return (bytes[offset] | (bytes[offset + 1] << 8)) & 0xffff;
  }

  private static toHex(value: number, width: number): string {
    return (value >>> 0).toString(16).padStart(width, '0');
  }
}


// Short ASCII strings, which include most ids and names, are built from their bytes with a loop,
// because a browser's TextDecoder costs more per call than the loop does at this length.
const ASCII_DECODE_MAX = 32;
const asciiScratch: number[] = [];

function decodeUtf8(bytes: Uint8Array, start: number, size: number): string {
  if (size === 0) {
    return '';
  }

  if (size <= ASCII_DECODE_MAX) {
    const units = asciiScratch;
    units.length = size;

    for (let i = 0; i < size; i++) {
      const byte = bytes[start + i];

      if (byte >= 0x80) {
        return utf8Decoder.decode(bytes.subarray(start, start + size));
      }

      units[i] = byte;
    }

    return String.fromCharCode.apply(null, units);
  }

  return utf8Decoder.decode(bytes.subarray(start, start + size));
}

// Where the last readVarUInt or readInlineValue call stopped. They return this second value here so
// that no call allocates an object. The readers are synchronous, so nothing runs between a call and
// the read of its end.
let varUIntEnd = 0;
let inlineValueEnd = 0;

function readVarUInt(data: Uint8Array, offset: number, limit: number, error: string): number {
  if (offset >= limit) {
    throw new Error(error);
  }

  const first = data[offset];

  if (first < 0x80) {
    varUIntEnd = offset + 1;
    return first;
  }

  let result = 0;
  let shift = 0;
  let bytesRead = 0;

  while (offset < limit) {
    const current = data[offset++];
    bytesRead++;

    // Only the low four bits of a fifth byte fit in 32; C# refuses the rest, and silently dropping
    // them here would decode a value the other stacks reject.
    if (shift === 28 && (current & 0x70) !== 0) {
      throw new Error('Teleport VarUInt exceeds 32-bit range');
    }

    result |= (current & 0x7f) << shift;

    if ((current & 0x80) === 0) {
      // Unsigned before measuring: a fifth byte sets bit 31, which reads as negative in a JS int.
      const value = result >>> 0;

      if (bytesRead !== getVarUIntLength(value)) {
        throw new Error('Teleport VarUInt is not canonical');
      }

      varUIntEnd = offset;
      return value;
    }

    shift += 7;

    if (shift >= 35) {
      throw new Error('Teleport VarUInt exceeds 32-bit range');
    }
  }

  throw new Error(error);
}

function writeVarUIntAt(bytes: Uint8Array, at: number, value: number): number {
  let current = value >>> 0;

  while (current >= 0x80) {
    bytes[at++] = (current & 0x7f) | 0x80;
    current >>>= 7;
  }

  bytes[at++] = current;
  return at;
}

function getVarUIntLength(value: number): number {
  if (value < 0x80) {
    return 1;
  }

  if (value < 0x4000) {
    return 2;
  }

  if (value < 0x20_0000) {
    return 3;
  }

  if (value < 0x1000_0000) {
    return 4;
  }

  return 5;
}

function composeDescriptor(type: TeleportType, flags = 0): number {
  if ((flags & 0xf0) !== 0) {
    throw new Error('Teleport flags must fit into 4 bits');
  }

  return ((type & 0x0f) << 4) | (flags & 0x0f);
}

function getFixedSize(type: TeleportType): number {
  switch (type) {
    case TeleportType.Int32:
    case TeleportType.UInt32:
    case TeleportType.Float32:
      return 4;
    case TeleportType.Int64:
    case TeleportType.UInt64:
    case TeleportType.Float64:
      return 8;
    case TeleportType.Guid:
      return 16;
    case TeleportType.Bool:
      return 1;
    case TeleportType.Null:
      return 0;
    default:
      return -1;
  }
}

function ensurePrimitiveKey(type: TeleportType): void {
  if (
    type === TeleportType.Array ||
    type === TeleportType.Object ||
    type === TeleportType.Dict ||
    type === TeleportType.Null
  ) {
    throw new Error('Dictionary keys must be primitive Teleport types');
  }
}

function ensureRange(limit: number, offset: number, length: number): void {
  if (offset < 0 || length < 0 || offset + length > limit) {
    throw new Error('Teleport payload exceeds bounds');
  }
}

function guidBytes(guid: TeleportGuid | Uint8Array): Uint8Array {
  const bytes = guid instanceof TeleportGuid ? guid.asBytes() : guid;

  if (bytes.length !== 16) {
    throw new Error('Guid payload must be 16 bytes');
  }

  return bytes;
}

/**
 * Finds the value of `type` at `offset` inside an array or dictionary. Inside them, a string, binary
 * or object has a length prefix and a nested array or dictionary does not. Returns where the value's bytes
 * start and sets {@link inlineValueEnd} to where they end.
 */
function readInlineValue(type: TeleportType, payload: Uint8Array, offset: number, limit: number, error: string, depth: number): number {
  const fixed = getFixedSize(type);

  if (fixed >= 0) {
    ensureRange(limit, offset, fixed);
    inlineValueEnd = offset + fixed;
    return offset;
  }

  switch (type) {
    case TeleportType.String:
    case TeleportType.Binary:
    case TeleportType.Object: {
      const length = readVarUInt(payload, offset, limit, error);
      const start = varUIntEnd;
      ensureRange(limit, start, length);
      inlineValueEnd = start + length;
      return start;
    }

    case TeleportType.Array: {
      const end = offset + consumeArrayPayload(payload, offset, limit, depth);
      inlineValueEnd = end;
      return offset;
    }

    case TeleportType.Dict: {
      const end = offset + consumeDictPayload(payload, offset, limit, depth);
      inlineValueEnd = end;
      return offset;
    }

    default:
      throw new Error(`Unsupported Teleport type ${TeleportType[type]}`);
  }
}

// Mirrors TeleportDepth.MaxDepth in the C# runtime: containers nested deeper than this throw a
// catchable error instead of recursing until the JS engine's stack limit aborts the frame. A
// hostile payload can reach this decoder without the server decoding it (an app that re-broadcasts
// client bytes without parsing them), so the bound has to live here too.
const MAX_CONTAINER_DEPTH = 128;

function consumeArrayPayload(payload: Uint8Array, start: number, limit: number, depth: number): number {
  if (depth >= MAX_CONTAINER_DEPTH) {
    throw new Error('Teleport nesting depth exceeded');
  }

  if (start >= limit) {
    throw new Error('Array payload exceeds bounds');
  }

  const descriptor = payload[start];
  const elementType = descriptor >> 4;

  if ((descriptor & 0x0f) !== 0) {
    throw new Error('Array flags must be zero');
  }

  const count = readVarUInt(payload, start + 1, limit, 'ArrayMalformed');
  let offset = varUIntEnd;
  const fixedSize = getFixedSize(elementType);

  if (fixedSize >= 0) {
    const total = fixedSize * count;
    ensureRange(limit, offset, total);
    return offset + total - start;
  }

  for (let i = 0; i < count; i++) {
    readInlineValue(elementType, payload, offset, limit, 'ArrayMalformed', depth + 1);
    offset = inlineValueEnd;
  }

  return offset - start;
}

function consumeDictPayload(payload: Uint8Array, start: number, limit: number, depth: number): number {
  if (depth >= MAX_CONTAINER_DEPTH) {
    throw new Error('Teleport nesting depth exceeded');
  }

  if (start + 2 > limit) {
    throw new Error('Dictionary payload too short');
  }

  const keyType = payload[start] >> 4;
  const valueType = payload[start + 1] >> 4;

  if ((payload[start] & 0x0f) !== 0 || (payload[start + 1] & 0x0f) !== 0) {
    throw new Error('Dictionary key/value flags must be zero');
  }

  ensurePrimitiveKey(keyType);
  const count = readVarUInt(payload, start + 2, limit, 'DictMalformed');
  let offset = varUIntEnd;

  for (let i = 0; i < count; i++) {
    readInlineValue(keyType, payload, offset, limit, 'DictMalformed', depth + 1);
    readInlineValue(valueType, payload, inlineValueEnd, limit, 'DictMalformed', depth + 1);
    offset = inlineValueEnd;
  }

  return offset - start;
}

const HEX = Array.from({ length: 256 }, (_, i) => i.toString(16).padStart(2, '0'));

function bytesToHex(bytes: Uint8Array): string {
  let hex = '';

  for (let i = 0; i < bytes.length; i++) {
    hex += HEX[bytes[i]];
  }

  return hex;
}

function toUint8Array(source: ProtocolMessage | ArrayBuffer | Uint8Array): Uint8Array {
  return source instanceof Uint8Array ? source : new Uint8Array(source);
}
