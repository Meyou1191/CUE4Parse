﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CUE4Parse.UE4.Assets.Exports.Animation.Codec
{
    using CUE4Parse.UE4.Objects.Core.Math;
    using CUE4Parse.UE4.Readers;
    using CUE4Parse.Utils;

    class AEFConstantKeyLerp : AEFConstantKeyLerpShared
    {
        public AEFConstantKeyLerp(AnimationCompressionFormat format)
            : base(format)
        {
        }
        public override void GetBoneAtomRotation(FArchive ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;
            var trackData = animData.CompressedTrackOffsets;
            int trackDataStartIndex = trackIndex * 4;
            int rotKeysOffset = trackData[trackDataStartIndex + 2];
            int numRotKeys = trackData[trackDataStartIndex + 3];

            ar.Position = rotKeysOffset;

            if (numRotKeys == 1)
            {
                AnimEncodingUtil.DecompressRotation(_format, ar, rotKeysOffset, out FQuat q);
                outAtom.Rotation = q;
            }
            else
            {
                float alpha = AnimEncodingUtil.TimeToIndex(
                    decompContext.SequenceLength,
                    decompContext.RelativePos,
                    numRotKeys,
                    decompContext.Interpolation,
                    out int index0,
                    out int index1);

                int rotStreamOffset = _format == AnimationCompressionFormat.ACF_IntervalFixed32NoW ? sizeof(float) * 6 : 0; // offset past Min and Range data
                if (index0 != index1)
                {
                    var keyData0Offset = rotKeysOffset + rotStreamOffset + index0 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    var keyData1Offset = rotKeysOffset + rotStreamOffset + index1 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    AnimEncodingUtil.DecompressRotation(_format, ar, keyData0Offset, out FQuat q1);
                    AnimEncodingUtil.DecompressRotation(_format, ar, keyData1Offset, out FQuat q2);
                    var rot = FQuat.FastLerp(q1, q2, alpha);
                    rot.Normalize();
                    outAtom.Rotation = rot;
                }
                else
                {
                    var keyDataOffset = rotKeysOffset + rotStreamOffset + index0 * AnimEncodingUtil.GetCompressedRotationStride(_format) * AnimEncodingUtil.GetCompressedRotationNum(_format);
                    AnimEncodingUtil.DecompressRotation(_format, ar, keyDataOffset, out FQuat q);
                    outAtom.Rotation = q;
                }

            }
        }

        public override void GetBoneAtomTranslation(FArchive ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;

            var trackData = animData.CompressedTrackOffsets;
            int trackDataStartIndex = trackIndex * 4;
            int transKeysOffset = trackData[trackDataStartIndex];
            int numTransKeys = trackData[trackDataStartIndex + 1];

            float alpha = AnimEncodingUtil.TimeToIndex(
                decompContext.SequenceLength,
                decompContext.RelativePos,
                numTransKeys,
                decompContext.Interpolation,
                out int index0,
                out int index1);

            ar.Position = transKeysOffset;
            int transStreamOffset = ((_format == AnimationCompressionFormat.ACF_IntervalFixed32NoW) && numTransKeys > 1) ? sizeof(float) * 6 : 0; // offset past Min and Range data

            if (index0 != index1)
            {
                var keyData0Offset = transKeysOffset + transStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                var keyData1Offset = transKeysOffset + transStreamOffset + index1 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressTranslation(_format, ar, keyData0Offset, out FVector v1);
               AnimEncodingUtil.DecompressTranslation(_format, ar, keyData1Offset, out FVector v2);
               outAtom.Translation = MathUtils.Lerp(v1, v2, alpha);
            }
            else
            {
                var keyDataOffset = transKeysOffset + transStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressTranslation(_format, ar, keyDataOffset, out FVector v);
                outAtom.Translation = v;
            }
        }

        public override void GetBoneAtomScale(FArchive ar, FAnimSequenceDecompressionContext decompContext, int trackIndex, ref FTransform outAtom)
        {
            var animData = (FUECompressedAnimData)decompContext.CompressedAnimData;

            int scaleKeysOffset = animData.CompressedScaleOffsets.GetOffsetData(trackIndex, 0);
            int numScaleKeys = animData.CompressedScaleOffsets.GetOffsetData(trackIndex, 1);
            ar.Position = scaleKeysOffset;

            float alpha = AnimEncodingUtil.TimeToIndex(
                decompContext.SequenceLength,
                decompContext.RelativePos,
                numScaleKeys,
                decompContext.Interpolation,
                out int index0,
                out int index1);

            int scaleStreamOffset = ((_format == AnimationCompressionFormat.ACF_IntervalFixed32NoW) && numScaleKeys > 1) ? sizeof(float) * 6 : 0; // offset past Min and Range data

            if (index0 != index1)
            {
                var keyData0Offset = scaleKeysOffset + scaleStreamOffset + index0 * AnimEncodingUtil.GetCompressedScaleStride(_format) * AnimEncodingUtil.GetCompressedScaleNum(_format);
                var keyData1Offset = scaleKeysOffset + scaleStreamOffset + index1 * AnimEncodingUtil.GetCompressedScaleStride(_format) * AnimEncodingUtil.GetCompressedScaleNum(_format);
                AnimEncodingUtil.DecompressScale(_format, ar, keyData0Offset, out FVector v1);
                AnimEncodingUtil.DecompressScale(_format, ar, keyData1Offset, out FVector v2);
                outAtom.Scale3D = MathUtils.Lerp(v1, v2, alpha);
            }
            else
            {
                var keyDataOffset = scaleKeysOffset + scaleStreamOffset + index0 * AnimEncodingUtil.GetCompressedTranslationStride(_format) * AnimEncodingUtil.GetCompressedTranslationNum(_format);
                AnimEncodingUtil.DecompressScale(_format, ar, keyDataOffset, out FVector v);
                outAtom.Translation = v;
            }
        }
    }
}