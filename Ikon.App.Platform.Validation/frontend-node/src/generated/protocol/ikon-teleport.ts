// noinspection JSUnusedGlobalSymbols

export const MinimumHeaderLength = 27;
export const ProtocolVersion = 1;

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

export function readProtocolMessageHeaders(raw: ProtocolMessage | ArrayBuffer | Uint8Array): ProtocolMessageHeaders {
  const bytes = toUint8Array(raw);
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);

  if (bytes.length < MinimumHeaderLength) {
    throw new Error('Protocol payload too short');
  }

  const length = view.getUint32(0, true);
  const opcode = view.getUint32(4, true);
  const senderId = view.getUint32(8, true);
  const trackId = view.getUint32(12, true);
  const sequenceId = view.getUint32(16, true);
  const targetCount = view.getUint32(20, true);
  const payloadVersion = view.getUint8(24);
  const payloadType = view.getUint8(25);
  const flags = view.getUint8(26);

  const expectedHeaderSize = MinimumHeaderLength + targetCount * 4;
  if (expectedHeaderSize > bytes.length) {
    throw new Error('Protocol header exceeds payload length');
  }

  const targetIds: number[] = [];
  let offset = MinimumHeaderLength;

  for (let i = 0; i < targetCount; i++) {
    targetIds.push(view.getUint32(offset, true));
    offset += 4;
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
  const headers = readProtocolMessageHeaders(bytes);

  if (expectedOpcode !== undefined && headers.opcode !== expectedOpcode) {
    throw new Error(`Unexpected opcode ${headers.opcode}`);
  }

  // As teleport is our main payload now, the message version does not really need enforcing.
  // if (expectedVersion !== undefined && headers.payloadVersion !== expectedVersion) {
  //   throw new Error(`Unexpected payload version ${headers.payloadVersion}`);
  // }

  if (headers.payloadType !== PayloadType.Teleport) {
    throw new Error(`Unexpected payload type ${headers.payloadType}`);
  }

  const headerSize = MinimumHeaderLength + headers.targetIds.length * 4;
  const payload = bytes.subarray(headerSize, headers.length);

  return payload;
}

export async function readProtocolMessagePayloadAsync(
  raw: ProtocolMessage | ArrayBuffer | Uint8Array,
  expectedOpcode?: number,
  _expectedVersion?: number,
): Promise<Uint8Array> {
  const bytes = toUint8Array(raw);
  const headers = readProtocolMessageHeaders(bytes);

  if (expectedOpcode !== undefined && headers.opcode !== expectedOpcode) {
    throw new Error(`Unexpected opcode ${headers.opcode}`);
  }

  if (headers.payloadType !== PayloadType.Teleport) {
    throw new Error(`Unexpected payload type ${headers.payloadType}`);
  }

  const headerSize = MinimumHeaderLength + headers.targetIds.length * 4;
  let payload = bytes.subarray(headerSize, headers.length);

  if ((headers.flags & MessageFlag.Compressed) !== 0) {
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

  const headerSize = MinimumHeaderLength + targetIds.length * 4;
  const totalSize = headerSize + payload.length;
  const buffer = new Uint8Array(totalSize);
  const view = new DataView(buffer.buffer);

  view.setUint32(0, totalSize, true);
  view.setUint32(4, opcode >>> 0, true);
  view.setUint32(8, senderId >>> 0, true);
  view.setUint32(12, trackId >>> 0, true);
  view.setUint32(16, sequenceId >>> 0, true);
  view.setUint32(20, targetIds.length >>> 0, true);
  view.setUint8(24, payloadVersion & 0xff);
  view.setUint8(25, payloadType & 0xff);
  view.setUint8(26, flags & 0xff);

  let offset = MinimumHeaderLength;
  for (let i = 0; i < targetIds.length; i++) {
    view.setUint32(offset, targetIds[i] >>> 0, true);
    offset += 4;
  }

  buffer.set(payload, headerSize);
  return asProtocolMessage(buffer);
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

  const headerSize = MinimumHeaderLength + targetIds.length * 4;
  const totalSize = headerSize + finalPayload.length;
  const buffer = new Uint8Array(totalSize);
  const view = new DataView(buffer.buffer);

  view.setUint32(0, totalSize, true);
  view.setUint32(4, opcode >>> 0, true);
  view.setUint32(8, senderId >>> 0, true);
  view.setUint32(12, trackId >>> 0, true);
  view.setUint32(16, sequenceId >>> 0, true);
  view.setUint32(20, targetIds.length >>> 0, true);
  view.setUint8(24, payloadVersion & 0xff);
  view.setUint8(25, payloadType & 0xff);
  view.setUint8(26, flags & 0xff);

  let offset = MinimumHeaderLength;
  for (let i = 0; i < targetIds.length; i++) {
    view.setUint32(offset, targetIds[i] >>> 0, true);
    offset += 4;
  }

  buffer.set(finalPayload, headerSize);
  return asProtocolMessage(buffer);
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

export class TeleportObjectWriter {
  private readonly buffer = new ByteBuffer();
  private cached?: Uint8Array;

  public constructor(version = 1) {
    this.buffer.writeByte(OBJECT_START);
    this.buffer.writeVarUInt(version >>> 0);
  }

  public writeInt32Field(fieldId: number, value: number): void {
    this.writeFixedField(fieldId, TeleportType.Int32, () => this.buffer.writeInt32(value | 0));
  }

  public writeUInt32Field(fieldId: number, value: number): void {
    this.writeFixedField(fieldId, TeleportType.UInt32, () => this.buffer.writeUInt32(value >>> 0));
  }

  public writeInt64Field(fieldId: number, value: bigint): void {
    this.writeFixedField(fieldId, TeleportType.Int64, () => this.buffer.writeBigInt64(value));
  }

  public writeUInt64Field(fieldId: number, value: bigint): void {
    this.writeFixedField(fieldId, TeleportType.UInt64, () => this.buffer.writeBigUInt64(value));
  }

  public writeFloat32Field(fieldId: number, value: number): void {
    this.writeFixedField(fieldId, TeleportType.Float32, () => this.buffer.writeFloat32(value));
  }

  public writeFloat64Field(fieldId: number, value: number): void {
    this.writeFixedField(fieldId, TeleportType.Float64, () => this.buffer.writeFloat64(value));
  }

  public writeBoolField(fieldId: number, value: boolean): void {
    this.writeFixedField(fieldId, TeleportType.Bool, () => this.buffer.writeByte(value ? 1 : 0));
  }

  public writeGuidField(fieldId: number, guid: TeleportGuid | Uint8Array): void {
    const bytes = guid instanceof TeleportGuid ? guid.asBytes() : guid;
    if (bytes.length !== 16) {
      throw new Error('Guid payload must be 16 bytes');
    }

    this.writeFixedField(fieldId, TeleportType.Guid, () => this.buffer.writeBytes(bytes));
  }

  public writeStringField(fieldId: number, value: string): void {
    const encoded = utf8Encoder.encode(value ?? '');
    this.writeVariableField(fieldId, TeleportType.String, encoded);
  }

  public writeBinaryField(fieldId: number, value: Uint8Array): void {
    this.writeVariableField(fieldId, TeleportType.Binary, value);
  }

  public writeObjectField(fieldId: number, version: number, build: (scope: TeleportObjectWriter) => void): void {
    const nested = new TeleportObjectWriter(version);
    build(nested);
    const payload = nested.finish();
    this.writeVariableField(fieldId, TeleportType.Object, payload);
  }

  public writeArrayField(fieldId: number, elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    const arrayWriter = new TeleportArrayWriter(elementType);
    build(arrayWriter);
    const payload = arrayWriter.finish();
    this.writeVariableField(fieldId, TeleportType.Array, payload);
  }

  public writeDictionaryField(
    fieldId: number,
    keyType: TeleportType,
    valueType: TeleportType,
    build: (scope: TeleportDictWriter) => void,
  ): void {
    const dictWriter = new TeleportDictWriter(keyType, valueType);
    build(dictWriter);
    const payload = dictWriter.finish();
    this.writeVariableField(fieldId, TeleportType.Dict, payload);
  }

  public finish(): Uint8Array {
    // The cached buffer is the closed signal: once it exists the object has been finished.
    if (!this.cached) {
      this.buffer.writeByte(OBJECT_END);
      this.cached = this.buffer.toUint8Array();
    }

    return this.cached;
  }

  private writeFixedField(fieldId: number, type: TeleportType, emit: () => void): void {
    this.writeFieldHeader(fieldId, type, 0);
    emit();
  }

  private writeVariableField(fieldId: number, type: TeleportType, payload: Uint8Array): void {
    this.writeFieldHeader(fieldId, type, payload.length);
    this.buffer.writeBytes(payload);
  }

  private writeFieldHeader(fieldId: number, type: TeleportType, payloadLength: number): void {
    this.buffer.writeUInt32(fieldId >>> 0);
    this.buffer.writeByte(composeDescriptor(type));

    if (requiresLengthEncoding(type)) {
      this.buffer.writeVarUInt(payloadLength >>> 0);
    }
  }
}

export class TeleportArrayWriter {
  private readonly payload = new ByteBuffer();
  private count = 0;

  public constructor(private readonly elementType: TeleportType) {}

  public writeInt32(value: number): void {
    this.ensureElementType(TeleportType.Int32);
    this.count++;
    this.payload.writeInt32(value | 0);
  }

  public writeUInt32(value: number): void {
    this.ensureElementType(TeleportType.UInt32);
    this.count++;
    this.payload.writeUInt32(value >>> 0);
  }

  public writeInt64(value: bigint): void {
    this.ensureElementType(TeleportType.Int64);
    this.count++;
    this.payload.writeBigInt64(value);
  }

  public writeUInt64(value: bigint): void {
    this.ensureElementType(TeleportType.UInt64);
    this.count++;
    this.payload.writeBigUInt64(value);
  }

  public writeFloat32(value: number): void {
    this.ensureElementType(TeleportType.Float32);
    this.count++;
    this.payload.writeFloat32(value);
  }

  public writeFloat64(value: number): void {
    this.ensureElementType(TeleportType.Float64);
    this.count++;
    this.payload.writeFloat64(value);
  }

  public writeBool(value: boolean): void {
    this.ensureElementType(TeleportType.Bool);
    this.count++;
    this.payload.writeByte(value ? 1 : 0);
  }

  public writeGuid(value: TeleportGuid | Uint8Array): void {
    this.ensureElementType(TeleportType.Guid);
    this.count++;
    const bytes = value instanceof TeleportGuid ? value.asBytes() : value;
    if (bytes.length !== 16) {
      throw new Error('Guid payload must be 16 bytes');
    }

    this.payload.writeBytes(bytes);
  }

  public writeString(value: string): void {
    this.ensureElementType(TeleportType.String);
    this.count++;
    const encoded = utf8Encoder.encode(value ?? '');
    this.payload.writeVarUInt(encoded.length);
    this.payload.writeBytes(encoded);
  }

  public writeBinary(value: Uint8Array): void {
    this.ensureElementType(TeleportType.Binary);
    this.count++;
    this.payload.writeVarUInt(value.length);
    this.payload.writeBytes(value);
  }

  public writeObject(version: number, build: (scope: TeleportObjectWriter) => void): void {
    this.ensureElementType(TeleportType.Object);
    this.count++;
    const nested = new TeleportObjectWriter(version);
    build(nested);
    const payload = nested.finish();
    this.payload.writeVarUInt(payload.length);
    this.payload.writeBytes(payload);
  }

  public writeArray(elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    this.ensureElementType(TeleportType.Array);
    this.count++;
    const nested = new TeleportArrayWriter(elementType);
    build(nested);
    const payload = nested.finish();
    this.payload.writeBytes(payload);
  }

  public writeDictionary(keyType: TeleportType, valueType: TeleportType, build: (scope: TeleportDictWriter) => void): void {
    this.ensureElementType(TeleportType.Dict);
    this.count++;
    const nested = new TeleportDictWriter(keyType, valueType);
    build(nested);
    const payload = nested.finish();
    this.payload.writeBytes(payload);
  }

  public finish(): Uint8Array {
    const buffer = new ByteBuffer();
    buffer.writeByte(composeDescriptor(this.elementType));
    buffer.writeVarUInt(this.count);
    buffer.writeBytes(this.payload.toUint8Array());
    return buffer.toUint8Array();
  }

  private ensureElementType(expected: TeleportType): void {
    if (this.elementType !== expected) {
      throw new Error(`Array element type is ${TeleportType[this.elementType]}, expected ${TeleportType[expected]}`);
    }
  }
}

export class TeleportDictWriter {
  private readonly payload = new ByteBuffer();
  private count = 0;
  private entryOpen = false;

  public constructor(private readonly keyType: TeleportType, private readonly valueType: TeleportType) {
    ensurePrimitiveKey(keyType);
  }

  public beginEntry(): TeleportDictEntryWriter {
    if (this.entryOpen) {
      throw new Error('Previous dictionary entry not completed');
    }

    this.count++;
    this.entryOpen = true;
    return new TeleportDictEntryWriter(this.keyType, this.valueType, this.payload, () => {
      this.entryOpen = false;
    });
  }

  public finish(): Uint8Array {
    if (this.entryOpen) {
      throw new Error('Dictionary entry not completed');
    }

    const buffer = new ByteBuffer();
    buffer.writeByte(composeDescriptor(this.keyType));
    buffer.writeByte(composeDescriptor(this.valueType));
    buffer.writeVarUInt(this.count);
    buffer.writeBytes(this.payload.toUint8Array());
    return buffer.toUint8Array();
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
  ) {}

  public writeKeyInt32(value: number): void {
    this.ensureKeyType(TeleportType.Int32);
    this.payload.writeInt32(value | 0);
    this.keyWritten = true;
  }

  public writeKeyUInt32(value: number): void {
    this.ensureKeyType(TeleportType.UInt32);
    this.payload.writeUInt32(value >>> 0);
    this.keyWritten = true;
  }

  public writeKeyInt64(value: bigint): void {
    this.ensureKeyType(TeleportType.Int64);
    this.payload.writeBigInt64(value);
    this.keyWritten = true;
  }

  public writeKeyUInt64(value: bigint): void {
    this.ensureKeyType(TeleportType.UInt64);
    this.payload.writeBigUInt64(value);
    this.keyWritten = true;
  }

  public writeKeyFloat32(value: number): void {
    this.ensureKeyType(TeleportType.Float32);
    this.payload.writeFloat32(value);
    this.keyWritten = true;
  }

  public writeKeyFloat64(value: number): void {
    this.ensureKeyType(TeleportType.Float64);
    this.payload.writeFloat64(value);
    this.keyWritten = true;
  }

  public writeKeyBool(value: boolean): void {
    this.ensureKeyType(TeleportType.Bool);
    this.payload.writeByte(value ? 1 : 0);
    this.keyWritten = true;
  }

  public writeKeyGuid(value: TeleportGuid | Uint8Array): void {
    this.ensureKeyType(TeleportType.Guid);
    const bytes = value instanceof TeleportGuid ? value.asBytes() : value;
    if (bytes.length !== 16) {
      throw new Error('Guid payload must be 16 bytes');
    }

    this.payload.writeBytes(bytes);
    this.keyWritten = true;
  }

  public writeKeyString(value: string): void {
    this.ensureKeyType(TeleportType.String);
    const encoded = utf8Encoder.encode(value ?? '');
    this.payload.writeVarUInt(encoded.length);
    this.payload.writeBytes(encoded);
    this.keyWritten = true;
  }

  public writeKeyBinary(value: Uint8Array): void {
    this.ensureKeyType(TeleportType.Binary);
    this.payload.writeVarUInt(value.length);
    this.payload.writeBytes(value);
    this.keyWritten = true;
  }

  public writeValueInt32(value: number): void {
    this.ensureValueType(TeleportType.Int32);
    this.payload.writeInt32(value | 0);
    this.valueWritten = true;
  }

  public writeValueUInt32(value: number): void {
    this.ensureValueType(TeleportType.UInt32);
    this.payload.writeUInt32(value >>> 0);
    this.valueWritten = true;
  }

  public writeValueInt64(value: bigint): void {
    this.ensureValueType(TeleportType.Int64);
    this.payload.writeBigInt64(value);
    this.valueWritten = true;
  }

  public writeValueUInt64(value: bigint): void {
    this.ensureValueType(TeleportType.UInt64);
    this.payload.writeBigUInt64(value);
    this.valueWritten = true;
  }

  public writeValueFloat32(value: number): void {
    this.ensureValueType(TeleportType.Float32);
    this.payload.writeFloat32(value);
    this.valueWritten = true;
  }

  public writeValueFloat64(value: number): void {
    this.ensureValueType(TeleportType.Float64);
    this.payload.writeFloat64(value);
    this.valueWritten = true;
  }

  public writeValueBool(value: boolean): void {
    this.ensureValueType(TeleportType.Bool);
    this.payload.writeByte(value ? 1 : 0);
    this.valueWritten = true;
  }

  public writeValueGuid(value: TeleportGuid | Uint8Array): void {
    this.ensureValueType(TeleportType.Guid);
    const bytes = value instanceof TeleportGuid ? value.asBytes() : value;
    if (bytes.length !== 16) {
      throw new Error('Guid payload must be 16 bytes');
    }

    this.payload.writeBytes(bytes);
    this.valueWritten = true;
  }

  public writeNullValue(): void {
    this.ensureValueType(TeleportType.Null);
    this.valueWritten = true;
  }

  public writeValueBinary(value: Uint8Array): void {
    this.ensureValueType(TeleportType.Binary);
    this.payload.writeVarUInt(value.length);
    this.payload.writeBytes(value);
    this.valueWritten = true;
  }

  public writeValueString(value: string): void {
    this.ensureValueType(TeleportType.String);
    const encoded = utf8Encoder.encode(value ?? '');
    this.payload.writeVarUInt(encoded.length);
    this.payload.writeBytes(encoded);
    this.valueWritten = true;
  }

  public writeValueObject(version: number, build: (scope: TeleportObjectWriter) => void): void {
    this.ensureValueType(TeleportType.Object);
    const nested = new TeleportObjectWriter(version);
    build(nested);
    const payload = nested.finish();
    this.payload.writeVarUInt(payload.length);
    this.payload.writeBytes(payload);
    this.valueWritten = true;
  }

  public writeValueArray(elementType: TeleportType, build: (scope: TeleportArrayWriter) => void): void {
    this.ensureValueType(TeleportType.Array);
    const nested = new TeleportArrayWriter(elementType);
    build(nested);
    const payload = nested.finish();
    this.payload.writeBytes(payload);
    this.valueWritten = true;
  }

  public writeValueDictionary(keyType: TeleportType, valueType: TeleportType, build: (scope: TeleportDictWriter) => void): void {
    this.ensureValueType(TeleportType.Dict);
    const nested = new TeleportDictWriter(keyType, valueType);
    build(nested);
    const payload = nested.finish();
    this.payload.writeBytes(payload);
    this.valueWritten = true;
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

  private ensureKeyType(expected: TeleportType): void {
    if (this.keyType !== expected) {
      throw new Error(`Dictionary key type is ${TeleportType[this.keyType]}, expected ${TeleportType[expected]}`);
    }
  }

  private ensureValueType(expected: TeleportType): void {
    if (this.valueType !== expected) {
      throw new Error(`Dictionary value type is ${TeleportType[this.valueType]}, expected ${TeleportType[expected]}`);
    }
  }
}

export class TeleportObjectReader {
  private offset: number;
  private readonly end: number;

  private constructor(private readonly buffer: Uint8Array, public readonly version: number, startOffset: number) {
    this.offset = startOffset;
    this.end = buffer.length - 1;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportObjectReader {
    const bytes = toUint8Array(data);
    if (bytes.length < 2) {
      throw new Error('Teleport payload too short');
    }

    if (bytes[0] !== OBJECT_START || bytes[bytes.length - 1] !== OBJECT_END) {
      throw new Error('Teleport object missing markers');
    }

    const state: OffsetState = { offset: 1 };
    const version = readVarUInt(bytes, state, 'InvalidLength');
    return new TeleportObjectReader(bytes, version, state.offset);
  }

  public next(): TeleportField | null {
    if (this.offset >= this.end) {
      return null;
    }

    if (this.offset + 5 > this.buffer.length) {
      throw new Error('Teleport object truncated');
    }

    const fieldId = readUInt32(this.buffer, this.offset);
    this.offset += 4;
    const descriptor = this.buffer[this.offset++];
    const type = ((descriptor >> 4) & 0x0f);

    if ((descriptor & 0x0f) !== 0) {
      throw new Error('Teleport field flags must be zero');
    }

    const fixedSize = getFixedSize(type);
    let payload: Uint8Array;

    if (fixedSize >= 0) {
      ensureRange(this.buffer, this.offset, fixedSize);
      payload = this.buffer.subarray(this.offset, this.offset + fixedSize);
      this.offset += fixedSize;
    } else {
      const state: OffsetState = { offset: this.offset };
      const length = readVarUInt(this.buffer, state, 'InvalidLength');
      ensureRange(this.buffer, state.offset, length);
      payload = this.buffer.subarray(state.offset, state.offset + length);
      this.offset = state.offset + length;
    }

    return new TeleportField(fieldId, type, payload);
  }
}

export class TeleportValue {
  public constructor(public readonly type: TeleportType, protected readonly payload: Uint8Array) {}

  public asInt32(): number {
    this.ensureType(TeleportType.Int32);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 4).getInt32(0, true);
  }

  public asUInt32(): number {
    this.ensureType(TeleportType.UInt32);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 4).getUint32(0, true);
  }

  public asInt64(): bigint {
    this.ensureType(TeleportType.Int64);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 8).getBigInt64(0, true);
  }

  public asUInt64(): bigint {
    this.ensureType(TeleportType.UInt64);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 8).getBigUint64(0, true);
  }

  public asFloat32(): number {
    this.ensureType(TeleportType.Float32);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 4).getFloat32(0, true);
  }

  public asFloat64(): number {
    this.ensureType(TeleportType.Float64);
    return new DataView(this.payload.buffer, this.payload.byteOffset, 8).getFloat64(0, true);
  }

  public asBool(): boolean {
    this.ensureType(TeleportType.Bool);
    return this.payload.length > 0 && this.payload[0] !== 0;
  }

  public asBinary(): Uint8Array {
    this.ensureType(TeleportType.Binary);
    return this.payload;
  }

  public asUtf8(): Uint8Array {
    this.ensureType(TeleportType.String);
    return this.payload;
  }

  public asString(): string {
    this.ensureType(TeleportType.String);
    return utf8Decoder.decode(this.payload);
  }

  public asGuid(): TeleportGuid {
    this.ensureType(TeleportType.Guid);
    return TeleportGuid.fromBytes(this.payload);
  }

  public asObject(): TeleportObjectReader {
    this.ensureType(TeleportType.Object);
    return TeleportObjectReader.create(this.payload);
  }

  public asArray(): TeleportArrayReader {
    this.ensureType(TeleportType.Array);
    return TeleportArrayReader.create(this.payload);
  }

  public asDictionary(): TeleportDictReader {
    this.ensureType(TeleportType.Dict);
    return TeleportDictReader.create(this.payload);
  }

  protected ensureType(expected: TeleportType): void {
    if (this.type !== expected) {
      throw new Error(`Teleport value has type ${TeleportType[this.type]}, expected ${TeleportType[expected]}`);
    }
  }
}

export class TeleportField extends TeleportValue {
  public constructor(public readonly fieldId: number, type: TeleportType, payload: Uint8Array) {
    super(type, payload);
  }

  public get isNull(): boolean {
    return this.type === TeleportType.Null;
  }
}

export class TeleportArrayReader {
  private readonly payload: Uint8Array;
  private readonly elementType: TeleportType;
  private readonly count: number;
  private offset: number;
  private index = 0;

  private constructor(payload: Uint8Array) {
    this.payload = payload;
    if (payload.length === 0) {
      throw new Error('Array payload too short');
    }

    const descriptor = payload[0];
    this.elementType = ((descriptor >> 4) & 0x0f);

    if ((descriptor & 0x0f) !== 0) {
      throw new Error('Array flags must be zero');
    }

    const state: OffsetState = { offset: 1 };
    this.count = readVarUInt(payload, state, 'ArrayMalformed');
    this.offset = state.offset;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportArrayReader {
    return new TeleportArrayReader(toUint8Array(data));
  }

  public next(): TeleportArrayElement | null {
    if (this.index >= this.count) {
      if (this.offset !== this.payload.length) {
        throw new Error('Array payload contains trailing data');
      }

      return null;
    }

    const value = this.readValue();
    this.index++;
    return value;
  }

  private readValue(): TeleportArrayElement {
    switch (this.elementType) {
      case TeleportType.Int32:
      case TeleportType.UInt32:
      case TeleportType.Float32:
      case TeleportType.Float64:
      case TeleportType.Bool:
      case TeleportType.Int64:
      case TeleportType.UInt64:
      case TeleportType.Guid: {
        const size = getFixedSize(this.elementType);
        ensureRange(this.payload, this.offset, size);
        const slice = this.payload.subarray(this.offset, this.offset + size);
        this.offset += size;
        return new TeleportArrayElement(this.elementType, slice);
      }

      case TeleportType.String:
      case TeleportType.Binary: {
        const state: OffsetState = { offset: this.offset };
        const length = readVarUInt(this.payload, state, 'ArrayMalformed');
        ensureRange(this.payload, state.offset, length);
        const slice = this.payload.subarray(state.offset, state.offset + length);
        this.offset = state.offset + length;
        return new TeleportArrayElement(this.elementType, slice);
      }

      case TeleportType.Object: {
        const state: OffsetState = { offset: this.offset };
        const length = readVarUInt(this.payload, state, 'ArrayMalformed');
        ensureRange(this.payload, state.offset, length);
        const slice = this.payload.subarray(state.offset, state.offset + length);
        this.offset = state.offset + length;
        return new TeleportArrayElement(TeleportType.Object, slice);
      }

      case TeleportType.Array: {
        const length = consumeArrayPayload(this.payload, this.offset);
        const slice = this.payload.subarray(this.offset, this.offset + length);
        this.offset += length;
        return new TeleportArrayElement(TeleportType.Array, slice);
      }

      case TeleportType.Dict: {
        const length = consumeDictPayload(this.payload, this.offset);
        const slice = this.payload.subarray(this.offset, this.offset + length);
        this.offset += length;
        return new TeleportArrayElement(TeleportType.Dict, slice);
      }

      default:
        throw new Error(`Unsupported array element type ${TeleportType[this.elementType]}`);
    }
  }
}

export class TeleportArrayElement extends TeleportValue {
  public constructor(type: TeleportType, payload: Uint8Array) {
    super(type, payload);
  }
}

export class TeleportDictReader {
  private readonly payload: Uint8Array;
  private readonly keyType: TeleportType;
  private readonly valueType: TeleportType;
  private readonly count: number;
  private offset: number;
  private index = 0;

  private constructor(payload: Uint8Array) {
    this.payload = payload;
    if (payload.length < 2) {
      throw new Error('Dictionary payload too short');
    }

    this.keyType = ((payload[0] >> 4) & 0x0f);
    this.valueType = ((payload[1] >> 4) & 0x0f);

    if ((payload[0] & 0x0f) !== 0 || (payload[1] & 0x0f) !== 0) {
      throw new Error('Dictionary key/value flags must be zero');
    }

    ensurePrimitiveKey(this.keyType);

    const state: OffsetState = { offset: 2 };
    this.count = readVarUInt(payload, state, 'DictMalformed');
    this.offset = state.offset;
  }

  public static create(data: ArrayBuffer | Uint8Array): TeleportDictReader {
    return new TeleportDictReader(toUint8Array(data));
  }

  public next(): TeleportDictEntry | null {
    if (this.index >= this.count) {
      if (this.offset !== this.payload.length) {
        throw new Error('Dictionary payload contains trailing data');
      }

      return null;
    }

    const key = this.readKey();
    const value = this.readValue();
    this.index++;
    return new TeleportDictEntry(key, value);
  }

  private readKey(): TeleportValue {
    const size = getFixedSize(this.keyType);

    if (size >= 0) {
      ensureRange(this.payload, this.offset, size);
      const slice = this.payload.subarray(this.offset, this.offset + size);
      this.offset += size;
      return new TeleportValue(this.keyType, slice);
    }

    if (this.keyType === TeleportType.String || this.keyType === TeleportType.Binary) {
      const state: OffsetState = { offset: this.offset };
      const length = readVarUInt(this.payload, state, 'DictMalformed');
      ensureRange(this.payload, state.offset, length);
      const slice = this.payload.subarray(state.offset, state.offset + length);
      this.offset = state.offset + length;
      return new TeleportValue(this.keyType, slice);
    }

    throw new Error('Unsupported dictionary key type');
  }

  private readValue(): TeleportValue {
    switch (this.valueType) {
      case TeleportType.String:
      case TeleportType.Binary: {
        const state: OffsetState = { offset: this.offset };
        const length = readVarUInt(this.payload, state, 'DictMalformed');
        ensureRange(this.payload, state.offset, length);
        const slice = this.payload.subarray(state.offset, state.offset + length);
        this.offset = state.offset + length;
        return new TeleportValue(this.valueType, slice);
      }

      case TeleportType.Object: {
        const state: OffsetState = { offset: this.offset };
        const length = readVarUInt(this.payload, state, 'DictMalformed');
        ensureRange(this.payload, state.offset, length);
        const slice = this.payload.subarray(state.offset, state.offset + length);
        this.offset = state.offset + length;
        return new TeleportValue(TeleportType.Object, slice);
      }

      case TeleportType.Array: {
        const length = consumeArrayPayload(this.payload, this.offset);
        const slice = this.payload.subarray(this.offset, this.offset + length);
        this.offset += length;
        return new TeleportValue(TeleportType.Array, slice);
      }

      case TeleportType.Dict: {
        const length = consumeDictPayload(this.payload, this.offset);
        const slice = this.payload.subarray(this.offset, this.offset + length);
        this.offset += length;
        return new TeleportValue(TeleportType.Dict, slice);
      }

      case TeleportType.Int32:
      case TeleportType.UInt32:
      case TeleportType.Float32:
      case TeleportType.Float64:
      case TeleportType.Bool:
      case TeleportType.Int64:
      case TeleportType.UInt64:
      case TeleportType.Guid:
      case TeleportType.Null: {
        const size = getFixedSize(this.valueType);
        ensureRange(this.payload, this.offset, size);
        const slice = this.payload.subarray(this.offset, this.offset + size);
        this.offset += size;
        return new TeleportValue(this.valueType, slice);
      }

      default:
        throw new Error(`Unsupported dictionary value type ${TeleportType[this.valueType]}`);
    }
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

    return new TeleportGuid(Uint8Array.from(bytes));
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

class ByteBuffer {
  private static readonly INITIAL_CAPACITY = 256;

  private buffer: Uint8Array;
  private dataView: DataView;
  private length = 0;

  constructor() {
    this.buffer = new Uint8Array(ByteBuffer.INITIAL_CAPACITY);
    this.dataView = new DataView(this.buffer.buffer);
  }

  private ensureCapacity(additional: number): void {
    const required = this.length + additional;
    if (required <= this.buffer.length) {
      return;
    }

    let newCapacity = this.buffer.length;
    while (newCapacity < required) {
      newCapacity *= 2;
    }

    const newBuffer = new Uint8Array(newCapacity);
    newBuffer.set(this.buffer.subarray(0, this.length));
    this.buffer = newBuffer;
    this.dataView = new DataView(this.buffer.buffer);
  }

  public writeByte(value: number): void {
    this.ensureCapacity(1);
    this.buffer[this.length++] = value & 0xff;
  }

  public writeBytes(value: Uint8Array): void {
    this.ensureCapacity(value.length);
    this.buffer.set(value, this.length);
    this.length += value.length;
  }

  public writeUInt32(value: number): void {
    this.ensureCapacity(4);
    this.dataView.setUint32(this.length, value >>> 0, true);
    this.length += 4;
  }

  public writeInt32(value: number): void {
    this.ensureCapacity(4);
    this.dataView.setInt32(this.length, value | 0, true);
    this.length += 4;
  }

  public writeFloat32(value: number): void {
    this.ensureCapacity(4);
    this.dataView.setFloat32(this.length, value, true);
    this.length += 4;
  }

  public writeFloat64(value: number): void {
    this.ensureCapacity(8);
    this.dataView.setFloat64(this.length, value, true);
    this.length += 8;
  }

  public writeBigInt64(value: bigint): void {
    this.ensureCapacity(8);
    this.dataView.setBigInt64(this.length, value, true);
    this.length += 8;
  }

  public writeBigUInt64(value: bigint): void {
    this.ensureCapacity(8);
    this.dataView.setBigUint64(this.length, value, true);
    this.length += 8;
  }

  public writeVarUInt(value: number): void {
    let current = value >>> 0;
    while (current >= 0x80) {
      this.ensureCapacity(1);
      this.buffer[this.length++] = (current & 0x7f) | 0x80;
      current >>>= 7;
    }

    this.ensureCapacity(1);
    this.buffer[this.length++] = current & 0x7f;
  }

  public toUint8Array(): Uint8Array {
    return this.buffer.slice(0, this.length);
  }
}

interface OffsetState {
  offset: number;
}

function readVarUInt(data: Uint8Array, state: OffsetState, error: string): number {
  if (state.offset >= data.length) {
    throw new Error(error);
  }

  let result = 0;
  let shift = 0;
  let bytesRead = 0;

  while (state.offset < data.length) {
    const current = data[state.offset++];
    bytesRead++;
    result |= (current & 0x7f) << shift;

    if ((current & 0x80) === 0) {
      if (bytesRead !== getVarUIntLength(result)) {
        throw new Error('Teleport VarUInt is not canonical');
      }

      return result >>> 0;
    }

    shift += 7;

    if (shift >= 35) {
      throw new Error('Teleport VarUInt exceeds 32-bit range');
    }
  }

  throw new Error(error);
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

function requiresLengthEncoding(type: TeleportType): boolean {
  return (
    type === TeleportType.String ||
    type === TeleportType.Binary ||
    type === TeleportType.Array ||
    type === TeleportType.Object ||
    type === TeleportType.Dict
  );
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

function ensureRange(data: Uint8Array, offset: number, length: number): void {
  if (offset < 0 || length < 0 || offset + length > data.length) {
    throw new Error('Teleport payload exceeds bounds');
  }
}

function readUInt32(buffer: Uint8Array, offset: number): number {
  return (
    buffer[offset] |
    (buffer[offset + 1] << 8) |
    (buffer[offset + 2] << 16) |
    (buffer[offset + 3] << 24)
  ) >>> 0;
}

function consumeArrayPayload(payload: Uint8Array, start: number): number {
  if (start >= payload.length) {
    throw new Error('Array payload exceeds bounds');
  }

  const descriptor = payload[start];
  const elementType = ((descriptor >> 4) & 0x0f);
  if ((descriptor & 0x0f) !== 0) {
    throw new Error('Array flags must be zero');
  }

  const state: OffsetState = { offset: start + 1 };
  const count = readVarUInt(payload, state, 'ArrayMalformed');
  const fixedSize = getFixedSize(elementType);

  if (fixedSize >= 0) {
    const total = fixedSize * count;
    ensureRange(payload, state.offset, total);
    return state.offset + total - start;
  }

  let offset = state.offset;
  for (let i = 0; i < count; i++) {
    offset = skipValue(elementType, payload, offset, 'ArrayMalformed');
  }

  return offset - start;
}

function consumeDictPayload(payload: Uint8Array, start: number): number {
  if (start + 2 > payload.length) {
    throw new Error('Dictionary payload too short');
  }

  const keyType = ((payload[start] >> 4) & 0x0f);
  const valueType = ((payload[start + 1] >> 4) & 0x0f);

  if ((payload[start] & 0x0f) !== 0 || (payload[start + 1] & 0x0f) !== 0) {
    throw new Error('Dictionary key/value flags must be zero');
  }

  ensurePrimitiveKey(keyType);

  const state: OffsetState = { offset: start + 2 };
  const count = readVarUInt(payload, state, 'DictMalformed');

  let offset = state.offset;
  for (let i = 0; i < count; i++) {
    offset = skipValue(keyType, payload, offset, 'DictMalformed');
    offset = skipValue(valueType, payload, offset, 'DictMalformed');
  }

  return offset - start;
}

function skipValue(type: TeleportType, payload: Uint8Array, offset: number, error: string): number {
  const fixed = getFixedSize(type);

  if (fixed >= 0) {
    ensureRange(payload, offset, fixed);
    return offset + fixed;
  }

  switch (type) {
    case TeleportType.String:
    case TeleportType.Binary: {
      const state: OffsetState = { offset };
      const length = readVarUInt(payload, state, error);
      ensureRange(payload, state.offset, length);
      return state.offset + length;
    }

    case TeleportType.Object: {
      const state: OffsetState = { offset };
      const length = readVarUInt(payload, state, error);
      ensureRange(payload, state.offset, length);
      return state.offset + length;
    }

    case TeleportType.Array:
      return offset + consumeArrayPayload(payload, offset);

    case TeleportType.Dict:
      return offset + consumeDictPayload(payload, offset);

    default:
      throw new Error(`Unsupported Teleport type ${TeleportType[type]}`);
  }
}

function bytesToHex(bytes: Uint8Array): string {
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

function toUint8Array(source: ProtocolMessage | ArrayBuffer | Uint8Array): Uint8Array {
  return source instanceof Uint8Array ? source : new Uint8Array(source);
}
